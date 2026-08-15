using System.Globalization;
using System.Net.Http;
using System.Text.Json;

namespace ClockWidg.Services;

/// <summary>One resolved place: what to label it, and the IANA zone its clock reads.</summary>
public sealed record ResolvedPlace(string Name, string Detail, string TimeZoneId);

/// <summary>
/// Turns what the user typed into a real city and its time zone, using the same
/// free, no-key services WeatherWidg resolves locations with:
///   • a US ZIP ("02134") or a postal code with a country ("SW1A 1AA, UK");
///   • an airport ICAO code ("KDEN", "EGLL");
///   • a town, or "town, region" ("Boston", "Springfield, IL", "Lyon, FR").
/// The name search carries the zone with it; the other two resolve a point first
/// and then ask what zone that point is in.
/// </summary>
public class GeoService
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

    public GeoService()
    {
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("ClockWidg/1.2 (+https://github.com/jgcoopersmith/ClockWidg)");
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    }

    public async Task<ResolvedPlace?> ResolveAsync(string query)
    {
        query = query.Trim();
        if (query.Length == 0) return null;

        // Bare US ZIP.
        if (query.Length == 5 && query.All(char.IsAsciiDigit))
            return await ResolvePostalAsync("us", query);

        // Four letters, no spaces: an ICAO code. Tried first — "EGLL" is not a town.
        if (query.Length == 4 && query.All(char.IsAsciiLetter))
        {
            var byIcao = await ResolveIcaoAsync(query.ToUpperInvariant());
            if (byIcao is not null) return byIcao;
        }

        // "code, CC" where CC is a two-letter country: a postal code elsewhere.
        int comma = query.LastIndexOf(',');
        if (comma > 0)
        {
            string head = query[..comma].Trim();
            string tail = query[(comma + 1)..].Trim();
            if (tail.Length == 2 && tail.All(char.IsAsciiLetter) && LooksPostal(head))
            {
                var byPostal = await ResolvePostalAsync(tail.ToLowerInvariant(), head);
                if (byPostal is not null) return byPostal;
                // Fall through: "Bath, UK" isn't a postal code, but "Bath" is a town.
            }
        }

        return await ResolveNameAsync(query);
    }

    /// <summary>Digits somewhere and short: a postal code rather than a town name.</summary>
    private static bool LooksPostal(string s) => s.Length <= 10 && s.Any(char.IsAsciiDigit);

    private async Task<ResolvedPlace?> ResolveNameAsync(string query)
    {
        // The geocoder searches on the town alone; a ", region" suffix becomes a filter on
        // the results, so "Springfield, IL" doesn't land on whichever Springfield is biggest.
        string name = query;
        string? region = null;
        int comma = query.IndexOf(',');
        if (comma > 0)
        {
            name = query[..comma].Trim();
            region = query[(comma + 1)..].Trim();
            if (region.Length == 0) region = null;
        }

        string url = "https://geocoding-api.open-meteo.com/v1/search" +
                     $"?name={Uri.EscapeDataString(name)}&count=10&language=en&format=json";
        using var doc = JsonDocument.Parse(await _http.GetStringAsync(url));

        if (!doc.RootElement.TryGetProperty("results", out var results)) return null;

        JsonElement? best = null;
        foreach (var r in results.EnumerateArray())
        {
            if (region is null) { best = r; break; }

            string admin1 = Str(r, "admin1");
            string country = Str(r, "country_code");
            string countryName = Str(r, "country");

            if (MatchesRegion(region, admin1, country, countryName)) { best = r; break; }
            best ??= r;   // keep the top hit as the fallback if nothing matches the region
        }
        if (best is not JsonElement hit) return null;

        string town = Str(hit, "name");
        if (town.Length == 0) town = name;

        // The geocoder hands back the IANA zone for the hit, which is the whole point
        // of resolving a city rather than picking a zone off a list.
        string zone = Str(hit, "timezone");
        if (zone.Length == 0)
        {
            double la = hit.GetProperty("latitude").GetDouble();
            double lo = hit.GetProperty("longitude").GetDouble();
            zone = await ZoneForPointAsync(la, lo) ?? "";
        }
        if (zone.Length == 0) return null;

        return new ResolvedPlace(town, Describe(town, Str(hit, "admin1"), Str(hit, "country")), zone);
    }

    private async Task<ResolvedPlace?> ResolvePostalAsync(string country, string code)
    {
        // People write "UK"; ISO (and Zippopotam) say "gb".
        if (country == "uk") country = "gb";

        // Zippopotam.us answers 404 for a code that doesn't exist — that's "no match",
        // not an outage. Postal formats often carry a space ("SW1A 1AA") and it mostly
        // indexes the outward part, so try the whole thing and then the first token.
        foreach (string attempt in new[] { code, code.Split(' ')[0] }.Distinct())
        {
            string json;
            try
            {
                json = await _http.GetStringAsync(
                    $"https://api.zippopotam.us/{country}/{Uri.EscapeDataString(attempt)}");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                continue;
            }

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("places", out var places) || places.GetArrayLength() == 0)
                continue;

            var p = places[0];
            string town = p.TryGetProperty("place name", out var pn) ? pn.GetString() ?? attempt : attempt;
            string state = p.TryGetProperty("state", out var st) ? st.GetString() ?? "" : "";

            // Invariant: the API speaks dot-decimal regardless of the machine's locale.
            double lat = double.Parse(p.GetProperty("latitude").GetString()!, CultureInfo.InvariantCulture);
            double lon = double.Parse(p.GetProperty("longitude").GetString()!, CultureInfo.InvariantCulture);

            string? zone = await ZoneForPointAsync(lat, lon);
            if (zone is null) return null;

            return new ResolvedPlace(town, Describe(town, state, country.ToUpperInvariant()), zone);
        }
        return null;
    }

    private async Task<ResolvedPlace?> ResolveIcaoAsync(string icao)
    {
        // aviationweather.gov's station index covers airports worldwide; an unknown
        // code comes back as an empty body (or empty array) rather than an error.
        string json = await _http.GetStringAsync(
            $"https://aviationweather.gov/api/data/stationinfo?ids={icao}&format=json");
        if (string.IsNullOrWhiteSpace(json)) return null;

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
            return null;

        var s = doc.RootElement[0];
        double lat = s.GetProperty("lat").GetDouble();
        double lon = s.GetProperty("lon").GetDouble();
        string site = Str(s, "site");
        if (site.Length == 0) site = icao;

        string? zone = await ZoneForPointAsync(lat, lon);
        if (zone is null) return null;

        return new ResolvedPlace(site, Describe(site, Str(s, "state"), icao), zone);
    }

    /// <summary>Asks Open-Meteo which zone a point sits in (timezone=auto echoes it back).</summary>
    private async Task<string?> ZoneForPointAsync(double lat, double lon)
    {
        string url = "https://api.open-meteo.com/v1/forecast" +
                     $"?latitude={Inv(lat)}&longitude={Inv(lon)}&timezone=auto&forecast_days=1";
        using var doc = JsonDocument.Parse(await _http.GetStringAsync(url));
        string zone = Str(doc.RootElement, "timezone");
        return zone.Length > 0 ? zone : null;
    }

    private static bool MatchesRegion(string wanted, string admin1, string countryCode, string countryName)
        => (admin1.Length > 0 && string.Equals(wanted, admin1, StringComparison.OrdinalIgnoreCase))
        || string.Equals(wanted, countryCode, StringComparison.OrdinalIgnoreCase)
        || (countryName.Length > 0 && string.Equals(wanted, countryName, StringComparison.OrdinalIgnoreCase))
        // "UK" is what people type; the geocoder says "GB".
        || (string.Equals(wanted, "UK", StringComparison.OrdinalIgnoreCase)
            && string.Equals(countryCode, "GB", StringComparison.OrdinalIgnoreCase))
        || (admin1.Length > 0 && admin1.StartsWith(wanted, StringComparison.OrdinalIgnoreCase));

    private static string Describe(string town, string region, string country)
    {
        var parts = new List<string> { town };
        if (region.Length > 0 && !string.Equals(region, town, StringComparison.OrdinalIgnoreCase))
            parts.Add(region);
        if (country.Length > 0) parts.Add(country);
        return string.Join(", ", parts);
    }

    private static string Str(JsonElement el, string name)
        => el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() ?? "" : "";

    private static string Inv(double d) => d.ToString(CultureInfo.InvariantCulture);
}

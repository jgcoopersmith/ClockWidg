namespace ClockWidg.Services;

/// <summary>
/// Resolves the IANA zone ids the geocoder returns ("Europe/London") against Windows,
/// which names its own zones differently ("GMT Standard Time"). .NET can look up IANA
/// ids directly on Windows, but older stored ids and Windows names still need the
/// conversion, so both routes are tried.
/// </summary>
public static class TimeZones
{
    public static bool TryResolve(string id, out TimeZoneInfo zone)
    {
        zone = TimeZoneInfo.Local;
        if (string.IsNullOrWhiteSpace(id)) return false;

        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch (TimeZoneNotFoundException) { }
        catch (InvalidTimeZoneException) { }

        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out string? windowsId) && windowsId is not null)
        {
            try
            {
                zone = TimeZoneInfo.FindSystemTimeZoneById(windowsId);
                return true;
            }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        return false;
    }

    /// <summary>The zone for an id, falling back to local time when it can't be resolved.</summary>
    public static TimeZoneInfo Resolve(string id)
        => TryResolve(id, out var zone) ? zone : TimeZoneInfo.Local;
}

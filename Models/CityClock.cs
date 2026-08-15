namespace ClockWidg.Models;

/// <summary>One city row in the location strip: a label plus the zone its time is read from.</summary>
public class CityClock
{
    public string Name { get; set; } = "";
    /// <summary>IANA zone id from the geocoder, e.g. "Europe/London".</summary>
    public string TimeZoneId { get; set; } = "";
    /// <summary>What the user typed, kept so the picker reopens on it.</summary>
    public string Query { get; set; } = "";
}

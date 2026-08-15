namespace ClockWidg.Models;

/// <summary>One city row in the location strip: a label plus the zone its time is read from.</summary>
public class CityClock
{
    public string Name { get; set; } = "";
    public string TimeZoneId { get; set; } = "";
}

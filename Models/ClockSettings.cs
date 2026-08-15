using System.Collections.ObjectModel;

namespace ClockWidg.Models;

public class ClockSettings
{
    public string FaceName { get; set; } = "AnalogClassic";
    public double WindowLeft { get; set; } = double.NaN;
    public double WindowTop { get; set; } = double.NaN;
    public double WindowWidth { get; set; } = 250;
    public double WindowHeight { get; set; } = 250;
    public bool AlwaysOnTop { get; set; } = false;
    public bool ShowSeconds { get; set; } = true;
    public bool Use24Hour { get; set; } = false;
    public bool ShowDate { get; set; } = true;
    public double WindowOpacity { get; set; } = 1.0;
    // "#RRGGBB", or null to use the current face's own default colour.
    public string? TimeColor { get; set; }
    public string? DateColor { get; set; }
    public string? BackgroundColor { get; set; }
    // Fades only the backdrop; WindowOpacity fades the whole widget.
    public double BackgroundOpacity { get; set; } = 1.0;
    // Fades only the time and date text.
    public double TextOpacity { get; set; } = 1.0;
    // null to use the current face's own default font.
    public FontChoice? TimeFont { get; set; }
    public FontChoice? DateFont { get; set; }
    public ObservableCollection<AlarmItem> Alarms { get; set; } = new();
    // Up to MaxLocations city clocks, shown stacked above the face.
    public List<CityClock> Locations { get; set; } = new();

    public const int MaxLocations = 3;
}

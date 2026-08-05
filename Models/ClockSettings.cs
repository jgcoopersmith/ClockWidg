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
    public double WindowOpacity { get; set; } = 1.0;
}

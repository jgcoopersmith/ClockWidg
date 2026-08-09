namespace ClockWidg.Models;

/// <summary>
/// A user-picked font for one element of a clock face. <see cref="Size"/> is the
/// starting size in DIPs — every face sits in a Viewbox, so it sets the element's
/// scale relative to the rest of the face rather than its final on-screen size.
/// </summary>
public class FontChoice
{
    public string Family { get; set; } = "Segoe UI";
    public double Size { get; set; } = 32;
    public bool Bold { get; set; }
    public bool Italic { get; set; }
}

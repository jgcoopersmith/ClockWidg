using System.Windows.Media;
using ClockWidg.Models;

namespace ClockWidg.Faces;

public interface IClockFace
{
    void UpdateTime(DateTime time, bool showSeconds, bool use24Hour);

    /// <summary>
    /// Applies user-chosen colours. A null colour means "use this face's own default".
    /// Faces that have no date element simply ignore <paramref name="dateColor"/>.
    /// <paramref name="backgroundColor"/> fills whatever each face treats as its
    /// backdrop — the rounded panel on the digital faces, the dial on the analog ones.
    /// </summary>
    void ApplyColors(Color? timeColor, Color? dateColor, Color? backgroundColor);

    /// <summary>
    /// Applies user-chosen fonts. A null choice means "use this face's own default".
    /// Faces with no time or date text (the analog faces, and Digital Binary) ignore both.
    /// </summary>
    void ApplyFonts(FontChoice? timeFont, FontChoice? dateFont);
}

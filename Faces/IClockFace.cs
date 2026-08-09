using System.Windows.Media;

namespace ClockWidg.Faces;

public interface IClockFace
{
    void UpdateTime(DateTime time, bool showSeconds, bool use24Hour);

    /// <summary>
    /// Applies user-chosen colours. A null colour means "use this face's own default".
    /// Faces that have no date element simply ignore <paramref name="dateColor"/>.
    /// </summary>
    void ApplyColors(Color? timeColor, Color? dateColor);
}

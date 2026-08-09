using System.Windows.Media;

namespace ClockWidg.Faces;

internal static class FaceBrush
{
    /// <summary>
    /// Builds a face's backdrop brush: the user's colour if they picked one, otherwise
    /// the face's designed brush, faded by <paramref name="opacity"/>. The opacity lives
    /// on the brush rather than the element so the time and date painted on top stay
    /// fully opaque.
    /// </summary>
    public static Brush? Background(Color? color, Brush? designed, double opacity)
    {
        Brush? brush = color is Color c ? new SolidColorBrush(c) : designed;
        if (brush is null || opacity >= 1.0) return brush;

        brush = brush.Clone(); // never mutate the designed brush — it may be shared or frozen
        brush.Opacity = Math.Clamp(opacity, 0.0, 1.0);
        return brush;
    }
}

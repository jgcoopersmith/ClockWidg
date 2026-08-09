using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ClockWidg.Models;

namespace ClockWidg.Faces;

public partial class AnalogMinimalFace : UserControl, IClockFace
{
    private DateTime _time = DateTime.Now;
    private bool _showSeconds = true;
    private bool _use24Hour = false;
    private Color? _timeColor;

    public AnalogMinimalFace() { InitializeComponent(); }

    public void UpdateTime(DateTime time, bool showSeconds, bool use24Hour)
    {
        _time = time; _showSeconds = showSeconds; _use24Hour = use24Hour;
        Redraw();
    }

    // This face draws no text at all, so no font applies.
    public void ApplyFonts(FontChoice? timeFont, FontChoice? dateFont) { }

    // Analog faces show no date, so only the hands take a colour.
    public void ApplyColors(Color? timeColor, Color? dateColor)
    {
        _timeColor = timeColor;
        Redraw();
    }

    private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e) => Redraw();

    private void Redraw()
    {
        ClockCanvas.Children.Clear();
        double w = ClockCanvas.ActualWidth, h = ClockCanvas.ActualHeight;
        if (w <= 0 || h <= 0) return;
        double cx = w / 2, cy = h / 2;
        double radius = Math.Min(cx, cy) * 0.88;
        double s = Math.Max(1.0, radius / 110.0); // stroke scale (1.0 at default size)

        // 12 hour dots
        var dotBrush = new SolidColorBrush(Color.FromRgb(200, 200, 220));
        for (int i = 0; i < 12; i++)
        {
            double angle = i * 30.0;
            var (dx, dy) = HandPoint(cx, cy, radius * 0.88, angle);
            double dotR = (i % 3 == 0 ? 4.5 : 2.5) * s;
            ClockCanvas.Children.Add(MakeEllipse(dx, dy, dotR, dotBrush));
        }

        // Hour hand — thin silver, or the user's chosen time colour
        Brush silverBrush = _timeColor is Color tc
            ? new SolidColorBrush(tc)
            : new SolidColorBrush(Color.FromRgb(180, 180, 200));
        double hourAngle = (_time.Hour % 12) * 30.0 + _time.Minute * 0.5;
        var (hx, hy) = HandPoint(cx, cy, radius * 0.55, hourAngle);
        ClockCanvas.Children.Add(MakeLine(cx, cy, hx, hy, silverBrush, 3 * s));

        // Minute hand
        double minAngle = _time.Minute * 6.0 + _time.Second * 0.1;
        var (mx, my) = HandPoint(cx, cy, radius * 0.80, minAngle);
        ClockCanvas.Children.Add(MakeLine(cx, cy, mx, my, silverBrush, 2 * s));

        // Second hand
        if (_showSeconds)
        {
            double secAngle = _time.Second * 6.0;
            var (sx, sy) = HandPoint(cx, cy, radius * 0.85, secAngle);
            var (stx, sty) = HandPoint(cx, cy, -radius * 0.15, secAngle);
            ClockCanvas.Children.Add(MakeLine(stx, sty, sx, sy, Brushes.Red, 1 * s));
        }

        // Center dot
        ClockCanvas.Children.Add(MakeEllipse(cx, cy, 4 * s, silverBrush));
    }

    private static (double x, double y) HandPoint(double cx, double cy, double r, double angleDeg)
    {
        double rad = (angleDeg - 90) * Math.PI / 180;
        return (cx + r * Math.Cos(rad), cy + r * Math.Sin(rad));
    }

    private static Line MakeLine(double x1, double y1, double x2, double y2, Brush stroke, double thickness)
        => new() { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = stroke, StrokeThickness = thickness, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };

    private static Ellipse MakeEllipse(double cx, double cy, double r, Brush fill)
    {
        var e = new Ellipse { Width = r * 2, Height = r * 2, Fill = fill };
        Canvas.SetLeft(e, cx - r);
        Canvas.SetTop(e, cy - r);
        return e;
    }
}

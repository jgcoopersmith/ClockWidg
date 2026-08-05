using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace ClockWidg.Faces;

public partial class AnalogNeonFace : UserControl, IClockFace
{
    private DateTime _time = DateTime.Now;
    private bool _showSeconds = true;
    private bool _use24Hour = false;

    public AnalogNeonFace() { InitializeComponent(); }

    public void UpdateTime(DateTime time, bool showSeconds, bool use24Hour)
    {
        _time = time; _showSeconds = showSeconds; _use24Hour = use24Hour;
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

        var cyanBrush = new SolidColorBrush(Color.FromRgb(0, 255, 255));
        var greenBrush = new SolidColorBrush(Color.FromRgb(57, 255, 20));
        var magentaBrush = new SolidColorBrush(Color.FromRgb(255, 0, 255));

        // Outer ring
        var ring = MakeEllipse(cx, cy, radius, Brushes.Transparent, cyanBrush, 1.5);
        ring.Effect = new DropShadowEffect { Color = Colors.Cyan, BlurRadius = 12, ShadowDepth = 0, Opacity = 0.8 };
        ClockCanvas.Children.Add(ring);

        // 12 hour position dots
        for (int i = 0; i < 12; i++)
        {
            double angle = i * 30.0;
            var (dx, dy) = HandPoint(cx, cy, radius * 0.83, angle);
            double dotR = i % 3 == 0 ? 5 : 3;
            var dot = MakeEllipse(dx, dy, dotR, cyanBrush);
            dot.Effect = new DropShadowEffect { Color = Colors.Cyan, BlurRadius = 15, ShadowDepth = 0, Opacity = 1 };
            ClockCanvas.Children.Add(dot);
        }

        // Hour hand - neon green
        double hourAngle = (_time.Hour % 12) * 30.0 + _time.Minute * 0.5;
        var (hx, hy) = HandPoint(cx, cy, radius * 0.55, hourAngle);
        var hourLine = MakeLine(cx, cy, hx, hy, greenBrush, 3);
        hourLine.Effect = new DropShadowEffect { Color = Color.FromRgb(57, 255, 20), BlurRadius = 14, ShadowDepth = 0, Opacity = 1 };
        ClockCanvas.Children.Add(hourLine);

        // Minute hand - neon green
        double minAngle = _time.Minute * 6.0 + _time.Second * 0.1;
        var (mx, my) = HandPoint(cx, cy, radius * 0.78, minAngle);
        var minLine = MakeLine(cx, cy, mx, my, greenBrush, 2);
        minLine.Effect = new DropShadowEffect { Color = Color.FromRgb(57, 255, 20), BlurRadius = 14, ShadowDepth = 0, Opacity = 1 };
        ClockCanvas.Children.Add(minLine);

        // Second hand - magenta
        if (_showSeconds)
        {
            double secAngle = _time.Second * 6.0;
            var (sx, sy) = HandPoint(cx, cy, radius * 0.82, secAngle);
            var (stx, sty) = HandPoint(cx, cy, -radius * 0.15, secAngle);
            var secLine = MakeLine(stx, sty, sx, sy, magentaBrush, 1.5);
            secLine.Effect = new DropShadowEffect { Color = Colors.Magenta, BlurRadius = 12, ShadowDepth = 0, Opacity = 1 };
            ClockCanvas.Children.Add(secLine);
        }

        // Center dot
        var center = MakeEllipse(cx, cy, 5, cyanBrush);
        center.Effect = new DropShadowEffect { Color = Colors.Cyan, BlurRadius = 10, ShadowDepth = 0, Opacity = 1 };
        ClockCanvas.Children.Add(center);
    }

    private static (double x, double y) HandPoint(double cx, double cy, double r, double angleDeg)
    {
        double rad = (angleDeg - 90) * Math.PI / 180;
        return (cx + r * Math.Cos(rad), cy + r * Math.Sin(rad));
    }

    private static Line MakeLine(double x1, double y1, double x2, double y2, Brush stroke, double thickness)
        => new() { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = stroke, StrokeThickness = thickness, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };

    private static Ellipse MakeEllipse(double cx, double cy, double r, Brush fill, Brush? stroke = null, double strokeThickness = 0)
    {
        var e = new Ellipse { Width = r * 2, Height = r * 2, Fill = fill };
        if (stroke != null) { e.Stroke = stroke; e.StrokeThickness = strokeThickness; }
        Canvas.SetLeft(e, cx - r);
        Canvas.SetTop(e, cy - r);
        return e;
    }
}

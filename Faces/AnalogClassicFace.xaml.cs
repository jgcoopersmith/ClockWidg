using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ClockWidg.Models;

namespace ClockWidg.Faces;

public partial class AnalogClassicFace : UserControl, IClockFace
{
    private DateTime _time = DateTime.Now;
    private bool _showSeconds = true;
    private bool _use24Hour = false;
    private Color? _timeColor;
    private Color? _backgroundColor;

    public AnalogClassicFace() { InitializeComponent(); }

    public void UpdateTime(DateTime time, bool showSeconds, bool use24Hour)
    {
        _time = time; _showSeconds = showSeconds; _use24Hour = use24Hour;
        Redraw();
    }

    // The dial labels are furniture rather than a time readout, so no font applies.
    public void ApplyFonts(FontChoice? timeFont, FontChoice? dateFont) { }

    // Analog faces show no date, so only the hands take a colour. The dial is
    // this face's backdrop, so the background colour fills it.
    public void ApplyColors(Color? timeColor, Color? dateColor, Color? backgroundColor)
    {
        _timeColor = timeColor;
        _backgroundColor = backgroundColor;
        Redraw();
    }

    private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e) => Redraw();

    private void Redraw()
    {
        ClockCanvas.Children.Clear();
        double w = ClockCanvas.ActualWidth, h = ClockCanvas.ActualHeight;
        if (w <= 0 || h <= 0) return;
        double cx = w / 2, cy = h / 2;
        double radius = Math.Min(cx, cy) * 0.9;
        double s = Math.Max(1.0, radius / 112.0); // stroke scale (1.0 at default size)

        // Face
        Brush dialBrush = _backgroundColor is Color bg ? new SolidColorBrush(bg) : Brushes.White;
        ClockCanvas.Children.Add(MakeEllipse(cx, cy, radius, dialBrush, Brushes.Black, 2 * s));

        // Tick marks
        for (int i = 0; i < 60; i++)
        {
            double angle = i * 6.0;
            bool isHour = i % 5 == 0;
            double inner = radius * (isHour ? 0.85 : 0.92);
            var (x1, y1) = HandPoint(cx, cy, inner, angle);
            var (x2, y2) = HandPoint(cx, cy, radius * 0.97, angle);
            ClockCanvas.Children.Add(MakeLine(x1, y1, x2, y2,
                Brushes.Black, (isHour ? 2.5 : 1.0) * s));
        }

        // Cardinal labels
        string[] labels = { "12", "3", "6", "9" };
        double[] labelAngles = { 0, 90, 180, 270 };
        double labelR = radius * 0.72;
        for (int i = 0; i < 4; i++)
        {
            var (lx, ly) = HandPoint(cx, cy, labelR, labelAngles[i]);
            var tb = new TextBlock
            {
                Text = labels[i],
                FontSize = radius * 0.13,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black
            };
            tb.Measure(new Size(100, 100));
            Canvas.SetLeft(tb, lx - tb.DesiredSize.Width / 2);
            Canvas.SetTop(tb, ly - tb.DesiredSize.Height / 2);
            ClockCanvas.Children.Add(tb);
        }

        Brush handBrush = _timeColor is Color tc ? new SolidColorBrush(tc) : Brushes.Black;

        // Hour hand
        double hourAngle = (_time.Hour % 12) * 30.0 + _time.Minute * 0.5;
        var (hx, hy) = HandPoint(cx, cy, radius * 0.55, hourAngle);
        ClockCanvas.Children.Add(MakeLine(cx, cy, hx, hy, handBrush, 4 * s));

        // Minute hand
        double minAngle = _time.Minute * 6.0 + _time.Second * 0.1;
        var (mx, my) = HandPoint(cx, cy, radius * 0.80, minAngle);
        ClockCanvas.Children.Add(MakeLine(cx, cy, mx, my, handBrush, 3 * s));

        // Second hand
        if (_showSeconds)
        {
            double secAngle = _time.Second * 6.0;
            var (sx, sy) = HandPoint(cx, cy, radius * 0.85, secAngle);
            var (stx, sty) = HandPoint(cx, cy, -radius * 0.15, secAngle);
            ClockCanvas.Children.Add(MakeLine(stx, sty, sx, sy, Brushes.Red, 1 * s));
        }

        // Center dot
        ClockCanvas.Children.Add(MakeEllipse(cx, cy, 5 * s, handBrush));
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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ClockWidg.Models;

namespace ClockWidg.Faces;

public partial class AnalogVintageFace : UserControl, IClockFace
{
    private DateTime _time = DateTime.Now;
    private bool _showSeconds = true;
    private bool _use24Hour = false;
    private Color? _timeColor;
    private Color? _backgroundColor;
    private double _backgroundOpacity = 1.0;

    private static readonly string[] RomanNumerals =
        { "XII", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI" };

    public AnalogVintageFace() { InitializeComponent(); }

    public void UpdateTime(DateTime time, bool showSeconds, bool use24Hour)
    {
        _time = time; _showSeconds = showSeconds; _use24Hour = use24Hour;
        Redraw();
    }

    // The Roman numerals are dial furniture rather than a time readout, so no font applies.
    public void ApplyFonts(FontChoice? timeFont, FontChoice? dateFont) { }

    // Analog faces show no date, so only the hands take a colour. The dial is
    // this face's backdrop, so the background colour fills it.
    public void ApplyColors(Color? timeColor, Color? dateColor, Color? backgroundColor, double backgroundOpacity, double textOpacity)
    {
        _timeColor = timeColor;
        _backgroundColor = backgroundColor;
        _backgroundOpacity = backgroundOpacity;
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

        var creamBrush = new SolidColorBrush(Color.FromRgb(245, 240, 224));
        var brownBrush = new SolidColorBrush(Color.FromRgb(90, 50, 20));
        Brush darkBrown = _timeColor is Color tc
            ? new SolidColorBrush(tc)
            : new SolidColorBrush(Color.FromRgb(60, 30, 10));

        // Face - cream with double ring
        Brush dialBrush = FaceBrush.Background(_backgroundColor, creamBrush, _backgroundOpacity)!;
        ClockCanvas.Children.Add(MakeEllipse(cx, cy, radius, dialBrush, brownBrush, 3 * s));
        ClockCanvas.Children.Add(MakeEllipse(cx, cy, radius * 0.95, null, brownBrush, 1 * s));

        // Tick marks
        for (int i = 0; i < 60; i++)
        {
            double angle = i * 6.0;
            bool isHour = i % 5 == 0;
            double inner = radius * (isHour ? 0.82 : 0.90);
            var (x1, y1) = HandPoint(cx, cy, inner, angle);
            var (x2, y2) = HandPoint(cx, cy, radius * 0.93, angle);
            ClockCanvas.Children.Add(MakeLine(x1, y1, x2, y2, brownBrush, (isHour ? 2 : 0.8) * s));
        }

        // Roman numerals - all 12
        double numR = radius * 0.70;
        double fontSize = radius * 0.11;
        for (int i = 0; i < 12; i++)
        {
            double angle = i * 30.0;
            var (nx, ny) = HandPoint(cx, cy, numR, angle);
            var tb = new TextBlock
            {
                Text = RomanNumerals[i],
                FontSize = fontSize,
                FontFamily = new FontFamily("Times New Roman"),
                FontStyle = FontStyles.Italic,
                Foreground = brownBrush
            };
            tb.Measure(new Size(200, 200));
            Canvas.SetLeft(tb, nx - tb.DesiredSize.Width / 2);
            Canvas.SetTop(tb, ny - tb.DesiredSize.Height / 2);
            ClockCanvas.Children.Add(tb);
        }

        // Hour hand - thick brown
        double hourAngle = (_time.Hour % 12) * 30.0 + _time.Minute * 0.5;
        var (hx, hy) = HandPoint(cx, cy, radius * 0.52, hourAngle);
        ClockCanvas.Children.Add(MakeLine(cx, cy, hx, hy, darkBrown, 5 * s));

        // Minute hand
        double minAngle = _time.Minute * 6.0 + _time.Second * 0.1;
        var (mx, my) = HandPoint(cx, cy, radius * 0.76, minAngle);
        ClockCanvas.Children.Add(MakeLine(cx, cy, mx, my, darkBrown, 3.5 * s));

        // Second hand
        if (_showSeconds)
        {
            double secAngle = _time.Second * 6.0;
            var (sx, sy) = HandPoint(cx, cy, radius * 0.82, secAngle);
            var (stx, sty) = HandPoint(cx, cy, -radius * 0.18, secAngle);
            var sepiaRed = new SolidColorBrush(Color.FromRgb(160, 60, 20));
            ClockCanvas.Children.Add(MakeLine(stx, sty, sx, sy, sepiaRed, 1.2 * s));
        }

        // Center pin
        ClockCanvas.Children.Add(MakeEllipse(cx, cy, 6 * s, brownBrush));
        ClockCanvas.Children.Add(MakeEllipse(cx, cy, 3 * s, creamBrush));
    }

    private static (double x, double y) HandPoint(double cx, double cy, double r, double angleDeg)
    {
        double rad = (angleDeg - 90) * Math.PI / 180;
        return (cx + r * Math.Cos(rad), cy + r * Math.Sin(rad));
    }

    private static Line MakeLine(double x1, double y1, double x2, double y2, Brush stroke, double thickness)
        => new() { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = stroke, StrokeThickness = thickness, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };

    private static Ellipse MakeEllipse(double cx, double cy, double r, Brush? fill, Brush? stroke = null, double strokeThickness = 0)
    {
        var e = new Ellipse { Width = r * 2, Height = r * 2, Fill = fill ?? Brushes.Transparent };
        if (stroke != null) { e.Stroke = stroke; e.StrokeThickness = strokeThickness; }
        Canvas.SetLeft(e, cx - r);
        Canvas.SetTop(e, cy - r);
        return e;
    }
}

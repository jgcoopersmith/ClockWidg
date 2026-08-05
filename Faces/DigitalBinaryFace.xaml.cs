using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ClockWidg.Faces;

public partial class DigitalBinaryFace : UserControl, IClockFace
{
    private DateTime _time = DateTime.Now;
    private bool _showSeconds = true;

    public DigitalBinaryFace() { InitializeComponent(); }

    public void UpdateTime(DateTime time, bool showSeconds, bool use24Hour)
    {
        _time = time; _showSeconds = showSeconds;
        Redraw();
    }

    private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e) => Redraw();

    private void Redraw()
    {
        ClockCanvas.Children.Clear();
        double w = ClockCanvas.ActualWidth, h = ClockCanvas.ActualHeight;
        if (w <= 0 || h <= 0) return;

        int cols = _showSeconds ? 3 : 2;
        int[] values = _showSeconds
            ? new[] { _time.Hour, _time.Minute, _time.Second }
            : new[] { _time.Hour, _time.Minute };
        string[] labels = _showSeconds ? new[] { "H", "M", "S" } : new[] { "H", "M" };

        int maxBits = 6; // max needed: seconds/minutes = 6 bits (0-59), hours = 5 bits (0-23)
        double margin = w * 0.05;
        double colW = (w - margin * 2) / cols;
        double labelH = h * 0.12;
        double dotAreaH = h - labelH - margin;
        double dotSpacing = dotAreaH / (maxBits + 0.5);
        double dotR = Math.Min(dotSpacing * 0.38, colW * 0.28);

        var onBrush = new SolidColorBrush(Color.FromRgb(0, 200, 80));
        var offBrush = new SolidColorBrush(Color.FromRgb(30, 50, 30));
        var labelBrush = new SolidColorBrush(Color.FromRgb(0, 160, 60));

        for (int c = 0; c < cols; c++)
        {
            double cx = margin + colW * c + colW / 2;
            int val = values[c];
            int bits = c == 0 ? 5 : 6; // hours need 5 bits, m/s need 6

            for (int b = 0; b < maxBits; b++)
            {
                double cy = margin + dotSpacing * (b + 0.5);
                int bitIndex = maxBits - 1 - b; // MSB at top
                bool isActive = b >= (maxBits - bits); // only show relevant bits
                bool isOn = isActive && ((val >> (bits - 1 - (b - (maxBits - bits)))) & 1) == 1;

                var dot = new Ellipse
                {
                    Width = dotR * 2,
                    Height = dotR * 2,
                    Fill = isActive ? (isOn ? onBrush : offBrush) : Brushes.Transparent
                };
                if (isActive && !isOn)
                {
                    dot.Stroke = offBrush;
                    dot.StrokeThickness = 1;
                }
                Canvas.SetLeft(dot, cx - dotR);
                Canvas.SetTop(dot, cy - dotR);
                ClockCanvas.Children.Add(dot);
            }

            // Column label
            var tb = new TextBlock
            {
                Text = labels[c],
                FontFamily = new FontFamily("Courier New"),
                FontSize = labelH * 0.65,
                Foreground = labelBrush,
                TextAlignment = TextAlignment.Center
            };
            tb.Measure(new Size(colW, labelH));
            Canvas.SetLeft(tb, cx - tb.DesiredSize.Width / 2);
            Canvas.SetTop(tb, h - labelH + (labelH - tb.DesiredSize.Height) / 2);
            ClockCanvas.Children.Add(tb);
        }
    }
}

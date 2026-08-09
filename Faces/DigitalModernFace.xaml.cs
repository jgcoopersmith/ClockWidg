using System.Windows.Controls;
using System.Windows.Media;

namespace ClockWidg.Faces;

public partial class DigitalModernFace : UserControl, IClockFace
{
    private readonly Brush _defaultTimeBrush;
    private readonly Brush _defaultDateBrush;

    public DigitalModernFace()
    {
        InitializeComponent();
        _defaultTimeBrush = TimeText.Foreground;
        _defaultDateBrush = DateText.Foreground;
    }

    public void UpdateTime(DateTime time, bool showSeconds, bool use24Hour)
    {
        string fmt = use24Hour
            ? (showSeconds ? "HH:mm:ss" : "HH:mm")
            : (showSeconds ? "h:mm:ss tt" : "h:mm tt");
        TimeText.Text = time.ToString(fmt);
        DateText.Text = time.ToString("dddd, MMMM d");
    }

    public void ApplyColors(Color? timeColor, Color? dateColor)
    {
        TimeText.Foreground = timeColor is Color t ? new SolidColorBrush(t) : _defaultTimeBrush;
        DateText.Foreground = dateColor is Color d ? new SolidColorBrush(d) : _defaultDateBrush;
    }
}

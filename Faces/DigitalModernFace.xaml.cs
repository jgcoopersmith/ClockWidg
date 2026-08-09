using System.Windows.Controls;
using System.Windows.Media;
using ClockWidg.Models;

namespace ClockWidg.Faces;

public partial class DigitalModernFace : UserControl, IClockFace
{
    private readonly Brush _defaultTimeBrush;
    private readonly Brush _defaultDateBrush;
    private readonly Brush _defaultPanelBrush;
    private readonly FontDefaults _timeFont;
    private readonly FontDefaults _dateFont;

    public DigitalModernFace()
    {
        InitializeComponent();
        _defaultTimeBrush = TimeText.Foreground;
        _defaultDateBrush = DateText.Foreground;
        _defaultPanelBrush = Panel.Background;
        _timeFont = FontDefaults.From(TimeText);
        _dateFont = FontDefaults.From(DateText);
    }

    public void ApplyFonts(FontChoice? timeFont, FontChoice? dateFont)
    {
        _timeFont.ApplyTo(TimeText, timeFont);
        _dateFont.ApplyTo(DateText, dateFont);
    }

    public void UpdateTime(DateTime time, bool showSeconds, bool use24Hour)
    {
        string fmt = use24Hour
            ? (showSeconds ? "HH:mm:ss" : "HH:mm")
            : (showSeconds ? "h:mm:ss tt" : "h:mm tt");
        TimeText.Text = time.ToString(fmt);
        DateText.Text = time.ToString("dddd, MMMM d");
    }

    public void ApplyColors(Color? timeColor, Color? dateColor, Color? backgroundColor)
    {
        TimeText.Foreground = timeColor is Color t ? new SolidColorBrush(t) : _defaultTimeBrush;
        DateText.Foreground = dateColor is Color d ? new SolidColorBrush(d) : _defaultDateBrush;
        Panel.Background = backgroundColor is Color b ? new SolidColorBrush(b) : _defaultPanelBrush;
    }
}

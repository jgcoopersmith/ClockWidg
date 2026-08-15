using System.Windows;
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

    public void SetHeaderInset(double dip)
    {
        Panel.Padding = new Thickness(0, dip, 0, 0);
        if (Panel.Child is Viewbox box)
        {
            box.Tag ??= box.Margin;   // remember the designed margin once
            var designed = (Thickness)box.Tag;

            // The face pads its own top to breathe against the panel edge. With a header
            // there, that padding is doubled separation — drop it while the header shows.
            box.Margin = new Thickness(designed.Left, dip > 0 ? 0 : designed.Top,
                                       designed.Right, designed.Bottom);

            // Uniform scaling leaves vertical slack, which the Viewbox centres — half of
            // it lands under the header and reads as a gap. Pin the content to the top.
            box.VerticalAlignment = dip > 0 ? VerticalAlignment.Top : VerticalAlignment.Stretch;
        }
    }

    public void ApplyFonts(FontChoice? timeFont, FontChoice? dateFont)
    {
        _timeFont.ApplyTo(TimeText, timeFont);
        _dateFont.ApplyTo(DateText, dateFont);
    }

    public void UpdateTime(DateTime time, bool showSeconds, bool use24Hour, bool showDate)
    {
        string fmt = use24Hour
            ? (showSeconds ? "HH:mm:ss" : "HH:mm")
            : (showSeconds ? "h:mm:ss tt" : "h:mm tt");
        TimeText.Text = time.ToString(fmt);
        DateText.Text = time.ToString("dddd, MMMM d");
        DateText.Visibility = showDate ? Visibility.Visible : Visibility.Collapsed;
    }

    public void ApplyColors(Color? timeColor, Color? dateColor, Color? backgroundColor, double backgroundOpacity, double textOpacity)
    {
        TimeText.Foreground = timeColor is Color t ? new SolidColorBrush(t) : _defaultTimeBrush;
        DateText.Foreground = dateColor is Color d ? new SolidColorBrush(d) : _defaultDateBrush;
        Panel.Background = FaceBrush.Background(backgroundColor, _defaultPanelBrush, backgroundOpacity);
        TimeText.Opacity = textOpacity;
        DateText.Opacity = textOpacity;
    }
}

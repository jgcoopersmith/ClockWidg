using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ClockWidg.Models;

namespace ClockWidg.Faces;

public partial class DigitalMatrixFace : UserControl, IClockFace
{
    private bool _cursorVisible = true;
    private readonly DispatcherTimer _blinkTimer = new();
    private DateTime _lastTime = DateTime.MinValue;
    private bool _showSeconds = true;
    private bool _use24Hour = false;

    private readonly Brush _defaultTimeBrush;
    private readonly Brush _defaultDateBrush;
    private readonly Brush _defaultYearBrush;
    private readonly Brush _defaultPanelBrush;
    private readonly FontDefaults _timeFont;
    private readonly FontDefaults _dateFont;
    private readonly FontDefaults _yearFont;

    public DigitalMatrixFace()
    {
        InitializeComponent();
        _defaultTimeBrush = Line1.Foreground;
        _defaultDateBrush = Line2.Foreground;
        _defaultYearBrush = Line3.Foreground;
        _defaultPanelBrush = Panel.Background;
        _timeFont = FontDefaults.From(Line1);
        _dateFont = FontDefaults.From(Line2);
        _yearFont = FontDefaults.From(Line3);
    }

    public void SetHeaderInset(double dip) => Panel.Padding = new Thickness(0, dip, 0, 0);

    public void ApplyFonts(FontChoice? timeFont, FontChoice? dateFont)
    {
        _timeFont.ApplyTo(Line1, timeFont);
        _dateFont.ApplyTo(Line2, dateFont);
        _yearFont.ApplyTo(Line3, dateFont);
    }

    // Line2 (day/month) and Line3 (year) are both part of the date.
    public void ApplyColors(Color? timeColor, Color? dateColor, Color? backgroundColor, double backgroundOpacity, double textOpacity)
    {
        Line1.Foreground = timeColor is Color t ? new SolidColorBrush(t) : _defaultTimeBrush;
        Panel.Background = FaceBrush.Background(backgroundColor, _defaultPanelBrush, backgroundOpacity);
        Line1.Opacity = Line2.Opacity = Line3.Opacity = textOpacity;
        if (dateColor is Color d)
        {
            Line2.Foreground = new SolidColorBrush(d);
            // Keep the year one shade dimmer, as the default palette does.
            Line3.Foreground = new SolidColorBrush(Color.FromArgb(0xB0, d.R, d.G, d.B));
        }
        else
        {
            Line2.Foreground = _defaultDateBrush;
            Line3.Foreground = _defaultYearBrush;
        }
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        _blinkTimer.Interval = TimeSpan.FromMilliseconds(500);
        _blinkTimer.Tick += (_, _) =>
        {
            _cursorVisible = !_cursorVisible;
            RefreshLine1();
        };
        _blinkTimer.Start();
    }

    public void UpdateTime(DateTime time, bool showSeconds, bool use24Hour, bool showDate)
    {
        _lastTime = time;
        _showSeconds = showSeconds;
        _use24Hour = use24Hour;
        RefreshLine1();
        Line2.Text = "  " + time.ToString("ddd MMM dd").ToUpper();
        Line3.Text = "  " + time.Year.ToString();
        // Both the day line and the year line are the date.
        Line2.Visibility = Line3.Visibility = showDate ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RefreshLine1()
    {
        if (_lastTime == DateTime.MinValue) return;
        string fmt = _use24Hour
            ? (_showSeconds ? "HH:mm:ss" : "HH:mm")
            : (_showSeconds ? "hh:mm:ss" : "hh:mm");
        string cursor = _cursorVisible ? "_" : " ";
        Line1.Text = "> " + _lastTime.ToString(fmt) + cursor;
    }
}

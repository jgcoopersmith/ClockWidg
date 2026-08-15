using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using ClockWidg.Models;

namespace ClockWidg.Faces;

public partial class DigitalNeonFace : UserControl, IClockFace
{
    private static readonly Color DefaultTimeGlow = Colors.Cyan;
    private static readonly Color DefaultDateGlow = Colors.Magenta;

    private readonly Brush _defaultTimeBrush;
    private readonly Brush _defaultDateBrush;
    private readonly Brush _defaultPanelBrush;
    private Color? _timeColor;
    private Color? _dateColor;
    private readonly FontDefaults _timeFont;
    private readonly FontDefaults _dateFont;

    public DigitalNeonFace()
    {
        InitializeComponent();
        _defaultTimeBrush = TimeText.Foreground;
        _defaultDateBrush = DateText.Foreground;
        _defaultPanelBrush = Panel.Background;
        _timeFont = FontDefaults.From(TimeText);
        _dateFont = FontDefaults.From(DateText);
    }

    public void SetHeaderInset(double dip) => Panel.Padding = new Thickness(0, dip, 0, 0);

    public void ApplyFonts(FontChoice? timeFont, FontChoice? dateFont)
    {
        _timeFont.ApplyTo(TimeText, timeFont);
        _dateFont.ApplyTo(DateText, dateFont);
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        TimeText.Effect = new DropShadowEffect
        {
            Color = _timeColor ?? DefaultTimeGlow,
            BlurRadius = 20,
            ShadowDepth = 0,
            Opacity = 1
        };
        DateText.Effect = new DropShadowEffect
        {
            Color = _dateColor ?? DefaultDateGlow,
            BlurRadius = 16,
            ShadowDepth = 0,
            Opacity = 1
        };
    }

    public void UpdateTime(DateTime time, bool showSeconds, bool use24Hour, bool showDate)
    {
        string fmt = use24Hour
            ? (showSeconds ? "HH:mm:ss" : "HH:mm")
            : (showSeconds ? "hh:mm:ss" : "hh:mm");
        TimeText.Text = time.ToString(fmt);
        DateText.Text = time.ToString("ddd  MMM dd  yyyy").ToUpper();
        DateText.Visibility = showDate ? Visibility.Visible : Visibility.Collapsed;
    }

    public void ApplyColors(Color? timeColor, Color? dateColor, Color? backgroundColor, double backgroundOpacity, double textOpacity)
    {
        _timeColor = timeColor;
        _dateColor = dateColor;

        Panel.Background = FaceBrush.Background(backgroundColor, _defaultPanelBrush, backgroundOpacity);
        // Element opacity here, so the neon glow fades with the text it belongs to.
        TimeText.Opacity = textOpacity;
        DateText.Opacity = textOpacity;

        TimeText.Foreground = timeColor is Color t ? new SolidColorBrush(t) : _defaultTimeBrush;
        DateText.Foreground = dateColor is Color d ? new SolidColorBrush(d) : _defaultDateBrush;

        // Keep the glow in step with the text so the neon look survives a recolour.
        if (TimeText.Effect is DropShadowEffect timeGlow)
            timeGlow.Color = timeColor ?? DefaultTimeGlow;
        if (DateText.Effect is DropShadowEffect dateGlow)
            dateGlow.Color = dateColor ?? DefaultDateGlow;
    }
}

using System.Windows.Controls;
using System.Windows.Media;
using ClockWidg.Models;

namespace ClockWidg.Faces;

public partial class DigitalWordFace : UserControl, IClockFace
{
    private static readonly string[] Ones =
    {
        "TWELVE", "ONE", "TWO", "THREE", "FOUR", "FIVE",
        "SIX", "SEVEN", "EIGHT", "NINE", "TEN", "ELEVEN",
        "TWELVE", "ONE", "TWO", "THREE", "FOUR", "FIVE",
        "SIX", "SEVEN", "EIGHT", "NINE", "TEN", "ELEVEN"
    };

    private static readonly string[] MinuteWords =
    {
        "", "FIVE", "TEN", "QUARTER", "TWENTY", "TWENTY FIVE", "HALF"
    };

    private readonly Brush _defaultTimeBrush;
    private readonly Brush _defaultPanelBrush;
    private readonly FontDefaults _timeFont;
    private readonly double _defaultLineHeight;
    private readonly double _lineHeightRatio;

    public DigitalWordFace()
    {
        InitializeComponent();
        _defaultTimeBrush = WordText.Foreground;
        _defaultPanelBrush = Panel.Background;
        _timeFont = FontDefaults.From(WordText);
        _defaultLineHeight = WordText.LineHeight;
        _lineHeightRatio = WordText.LineHeight / WordText.FontSize;
    }

    // This face shows no date, so dateFont has nothing to apply to.
    public void ApplyFonts(FontChoice? timeFont, FontChoice? dateFont)
    {
        _timeFont.ApplyTo(WordText, timeFont);
        // The line height is fixed in XAML, so scale it with the chosen size
        // or the three lines would overlap.
        WordText.LineHeight = timeFont is null || timeFont.Size <= 0
            ? _defaultLineHeight
            : timeFont.Size * _lineHeightRatio;
    }

    public void UpdateTime(DateTime time, bool showSeconds, bool use24Hour)
    {
        WordText.Text = ToWords(time);
    }

    // This face shows no date, so dateColor has nothing to apply to.
    public void ApplyColors(Color? timeColor, Color? dateColor, Color? backgroundColor, double backgroundOpacity, double textOpacity)
    {
        WordText.Foreground = timeColor is Color t ? new SolidColorBrush(t) : _defaultTimeBrush;
        Panel.Background = FaceBrush.Background(backgroundColor, _defaultPanelBrush, backgroundOpacity);
        WordText.Opacity = textOpacity;
    }

    private static string ToWords(DateTime time)
    {
        int h = time.Hour;
        int m = time.Minute;
        int rounded = ((m + 2) / 5) * 5; // round to nearest 5 min

        if (rounded == 60) { rounded = 0; h++; }

        int displayHour = h % 12 == 0 ? 12 : h % 12;
        int nextHour = (h + 1) % 12 == 0 ? 12 : (h + 1) % 12;

        if (rounded == 0)
            return $"IT IS\n{Ones[displayHour]}\nO'CLOCK";
        else if (rounded == 30)
            return $"IT IS\nHALF PAST\n{Ones[displayHour]}";
        else if (rounded < 35)
        {
            string mins = MinuteWords[rounded / 5];
            return $"IT IS\n{mins}\nPAST {Ones[displayHour]}";
        }
        else
        {
            int minsTo = 60 - rounded;
            string mins = MinuteWords[minsTo / 5];
            return $"IT IS\n{mins}\nTO {Ones[nextHour]}";
        }
    }
}

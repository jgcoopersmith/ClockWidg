using System.Windows.Controls;

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

    public DigitalWordFace() { InitializeComponent(); }

    public void UpdateTime(DateTime time, bool showSeconds, bool use24Hour)
    {
        WordText.Text = ToWords(time);
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

using System.Windows.Controls;

namespace ClockWidg.Faces;

public partial class DigitalModernFace : UserControl, IClockFace
{
    public DigitalModernFace() { InitializeComponent(); }

    public void UpdateTime(DateTime time, bool showSeconds, bool use24Hour)
    {
        string fmt = use24Hour
            ? (showSeconds ? "HH:mm:ss" : "HH:mm")
            : (showSeconds ? "h:mm:ss tt" : "h:mm tt");
        TimeText.Text = time.ToString(fmt);
        DateText.Text = time.ToString("dddd, MMMM d");
    }
}

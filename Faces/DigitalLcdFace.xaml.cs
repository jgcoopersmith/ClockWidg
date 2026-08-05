using System.Windows.Controls;

namespace ClockWidg.Faces;

public partial class DigitalLcdFace : UserControl, IClockFace
{
    public DigitalLcdFace() { InitializeComponent(); }

    public void UpdateTime(DateTime time, bool showSeconds, bool use24Hour)
    {
        string fmt = use24Hour
            ? (showSeconds ? "HH:mm:ss" : "HH:mm")
            : (showSeconds ? "hh:mm:ss tt" : "hh:mm tt");
        TimeText.Text = time.ToString(fmt);
        DateText.Text = time.ToString("ddd MMM dd  yyyy").ToUpper();
    }
}

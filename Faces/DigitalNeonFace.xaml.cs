using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace ClockWidg.Faces;

public partial class DigitalNeonFace : UserControl, IClockFace
{
    public DigitalNeonFace() { InitializeComponent(); }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        TimeText.Effect = new DropShadowEffect
        {
            Color = Colors.Cyan,
            BlurRadius = 20,
            ShadowDepth = 0,
            Opacity = 1
        };
        DateText.Effect = new DropShadowEffect
        {
            Color = Colors.Magenta,
            BlurRadius = 16,
            ShadowDepth = 0,
            Opacity = 1
        };
    }

    public void UpdateTime(DateTime time, bool showSeconds, bool use24Hour)
    {
        string fmt = use24Hour
            ? (showSeconds ? "HH:mm:ss" : "HH:mm")
            : (showSeconds ? "hh:mm:ss" : "hh:mm");
        TimeText.Text = time.ToString(fmt);
        DateText.Text = time.ToString("ddd  MMM dd  yyyy").ToUpper();
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ClockWidg.Faces;

public partial class DigitalMatrixFace : UserControl, IClockFace
{
    private bool _cursorVisible = true;
    private readonly DispatcherTimer _blinkTimer = new();
    private DateTime _lastTime = DateTime.MinValue;
    private bool _showSeconds = true;
    private bool _use24Hour = false;

    public DigitalMatrixFace() { InitializeComponent(); }

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

    public void UpdateTime(DateTime time, bool showSeconds, bool use24Hour)
    {
        _lastTime = time;
        _showSeconds = showSeconds;
        _use24Hour = use24Hour;
        RefreshLine1();
        Line2.Text = "  " + time.ToString("ddd MMM dd").ToUpper();
        Line3.Text = "  " + time.Year.ToString();
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

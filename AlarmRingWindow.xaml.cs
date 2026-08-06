using System.Windows;
using ClockWidg.Services;

namespace ClockWidg;

public partial class AlarmRingWindow : Window
{
    private readonly AlertSound _sound = new();

    /// <summary>Raised with the snooze duration (minutes) when the user snoozes.</summary>
    public event Action<int>? SnoozeRequested;

    public AlarmRingWindow(string name, string timeText)
    {
        InitializeComponent();
        NameText.Text = string.IsNullOrWhiteSpace(name) ? "Alarm" : name;
        TimeText.Text = timeText;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Activate();
        _sound.Start();
    }

    private void Snooze_Click(object sender, RoutedEventArgs e)
    {
        SnoozeRequested?.Invoke(9);
        Close();
    }

    private void Dismiss_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_Closed(object sender, EventArgs e) => _sound.Dispose();
}

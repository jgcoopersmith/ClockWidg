using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ClockWidg.Models;
using ClockWidg.Services;

namespace ClockWidg;

public partial class ToolsWindow : Window
{
    private readonly ClockSettings _settings;
    private readonly Action _save;

    // Combo sources
    public int[] HoursList { get; } = Enumerable.Range(0, 24).ToArray();
    public int[] MinutesList { get; } = Enumerable.Range(0, 60).ToArray();
    public ObservableCollection<AlarmItem> Alarms => _settings.Alarms;

    // Stopwatch
    private readonly System.Diagnostics.Stopwatch _sw = new();
    private readonly DispatcherTimer _swTimer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private int _lapCount;
    private TimeSpan _lastLap = TimeSpan.Zero;

    // Timer
    private readonly DispatcherTimer _tmTimer = new() { Interval = TimeSpan.FromMilliseconds(200) };
    private DateTime _tmEnd;
    private TimeSpan _tmRemaining = TimeSpan.FromMinutes(5);
    private bool _tmRunning;
    private readonly AlertSound _tmSound = new();

    public ToolsWindow(ClockSettings settings, Action saveCallback)
    {
        InitializeComponent();
        _settings = settings;
        _save = saveCallback;
        DataContext = this;

        _swTimer.Tick += (_, _) => SwDisplay.Text = Format(_sw.Elapsed);
        _tmTimer.Tick += TmTimer_Tick;

        UpdateEmptyState();
    }

    public void ShowTab(string tab)
    {
        Tabs.SelectedIndex = tab switch
        {
            "Stopwatch" => 1,
            "Timer" => 2,
            _ => 0,
        };
    }

    // ---------------- Alarms ----------------
    private void AddAlarm_Click(object sender, RoutedEventArgs e)
    {
        var now = DateTime.Now;
        _settings.Alarms.Add(new AlarmItem
        {
            Name = $"Alarm {_settings.Alarms.Count + 1}",
            Hour = now.Hour,
            Minute = now.Minute,
            Enabled = true,
        });
        UpdateEmptyState();
        _save();
    }

    private void DeleteAlarm_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is AlarmItem item)
        {
            _settings.Alarms.Remove(item);
            UpdateEmptyState();
            _save();
        }
    }

    // Fired by name/time/enable/repeat edits
    private void AlarmChanged_Update(object sender, RoutedEventArgs e) => _save();

    private void UpdateEmptyState()
    {
        EmptyAlarmsText.Visibility = _settings.Alarms.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    // ---------------- Stopwatch ----------------
    private void SwStartStop_Click(object sender, RoutedEventArgs e)
    {
        if (_sw.IsRunning)
        {
            _sw.Stop();
            _swTimer.Stop();
            SwStartStop.Content = "Start";
            SwDisplay.Text = Format(_sw.Elapsed);
        }
        else
        {
            _sw.Start();
            _swTimer.Start();
            SwStartStop.Content = "Stop";
        }
        SwLap.IsEnabled = _sw.IsRunning;
        SwReset.IsEnabled = _sw.Elapsed > TimeSpan.Zero;
    }

    private void SwLap_Click(object sender, RoutedEventArgs e)
    {
        _lapCount++;
        TimeSpan total = _sw.Elapsed;
        TimeSpan split = total - _lastLap;
        _lastLap = total;
        SwLaps.Items.Insert(0, $"Lap {_lapCount,2}   +{Format(split)}   {Format(total)}");
    }

    private void SwReset_Click(object sender, RoutedEventArgs e)
    {
        _sw.Reset();
        _swTimer.Stop();
        _lapCount = 0;
        _lastLap = TimeSpan.Zero;
        SwLaps.Items.Clear();
        SwDisplay.Text = Format(TimeSpan.Zero);
        SwStartStop.Content = "Start";
        SwLap.IsEnabled = false;
        SwReset.IsEnabled = false;
    }

    private static string Format(TimeSpan t) =>
        $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}.{t.Milliseconds / 10:00}";

    // ---------------- Timer ----------------
    private void TmStartPause_Click(object sender, RoutedEventArgs e)
    {
        StopTimerAlarm();

        if (_tmRunning)
        {
            // pause
            _tmRunning = false;
            _tmTimer.Stop();
            _tmRemaining = _tmEnd - DateTime.Now;
            if (_tmRemaining < TimeSpan.Zero) _tmRemaining = TimeSpan.Zero;
            TmStartPause.Content = "Resume";
            TimerInputs.IsEnabled = true;
            return;
        }

        // start / resume
        if (TmStartPause.Content as string == "Start")
        {
            var dur = new TimeSpan(TmHours.SelectedIndex, TmMinutes.SelectedIndex, TmSeconds.SelectedIndex);
            if (dur <= TimeSpan.Zero) return;
            _tmRemaining = dur;
        }

        _tmEnd = DateTime.Now + _tmRemaining;
        _tmRunning = true;
        _tmTimer.Start();
        TmStartPause.Content = "Pause";
        TmReset.IsEnabled = true;
        TimerInputs.IsEnabled = false;
        UpdateTimerDisplay(_tmRemaining);
    }

    private void TmTimer_Tick(object? sender, EventArgs e)
    {
        TimeSpan remaining = _tmEnd - DateTime.Now;
        if (remaining <= TimeSpan.Zero)
        {
            remaining = TimeSpan.Zero;
            _tmTimer.Stop();
            _tmRunning = false;
            UpdateTimerDisplay(remaining);
            TimerFinished();
            return;
        }
        UpdateTimerDisplay(remaining);
    }

    private void TimerFinished()
    {
        TmDoneText.Visibility = Visibility.Visible;
        TmStartPause.Content = "Start";
        TmStartPause.IsEnabled = false;
        _tmSound.Start();
        if (!IsActive) { Activate(); }
    }

    private void TmReset_Click(object sender, RoutedEventArgs e)
    {
        StopTimerAlarm();
        _tmTimer.Stop();
        _tmRunning = false;
        _tmRemaining = new TimeSpan(TmHours.SelectedIndex, TmMinutes.SelectedIndex, TmSeconds.SelectedIndex);
        if (_tmRemaining <= TimeSpan.Zero) _tmRemaining = TimeSpan.FromMinutes(5);
        UpdateTimerDisplay(_tmRemaining);
        TmStartPause.Content = "Start";
        TmStartPause.IsEnabled = true;
        TmReset.IsEnabled = false;
        TimerInputs.IsEnabled = true;
    }

    private void StopTimerAlarm()
    {
        if (_tmSound.IsPlaying) _tmSound.Stop();
        TmDoneText.Visibility = Visibility.Collapsed;
        TmStartPause.IsEnabled = true;
    }

    private void UpdateTimerDisplay(TimeSpan t) =>
        TmDisplay.Text = $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";

    // ---------------- lifetime ----------------
    private void Window_Closed(object? sender, EventArgs e)
    {
        _swTimer.Stop();
        _tmTimer.Stop();
        _tmSound.Dispose();
        _save();
    }
}

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using ClockWidg.Faces;
using ClockWidg.Models;
using ClockWidg.Services;

namespace ClockWidg;

public partial class MainWindow : Window
{
    private readonly SettingsService _settingsService = new();
    private ClockSettings _settings = new();
    private readonly DispatcherTimer _timer = new();
    private IClockFace? _currentFace;

    private ToolsWindow? _toolsWindow;
    private DateTime _lastAlarmCheck = DateTime.Now;
    private readonly List<(DateTime When, string Name)> _snoozes = new();

    public MainWindow()
    {
        InitializeComponent();
        _settings = _settingsService.Load();
        ApplySettings();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += Timer_Tick;
        _timer.Start();
        Loaded += (_, _) => RefreshFace();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        DateTime now = DateTime.Now;
        _currentFace?.UpdateTime(now, _settings.ShowSeconds, _settings.Use24Hour, _settings.ShowDate);
        ReassertTopmost();
        CheckAlarms(now);
    }

    /// <summary>
    /// WPF's Topmost is set once and then quietly lost — a full-screen app, a UAC
    /// prompt or another topmost window can demote us, and nothing puts it back.
    /// While Always On Top is ticked, push the window back into the topmost band
    /// each tick. SWP_NOACTIVATE means this never steals focus from what you're using.
    /// </summary>
    private void ReassertTopmost()
    {
        if (!_settings.AlwaysOnTop) return;

        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;

        if (!Topmost) Topmost = true;
        NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
    }

    private void ApplySettings()
    {
        if (!double.IsNaN(_settings.WindowLeft) && !double.IsNaN(_settings.WindowTop))
        {
            Left = _settings.WindowLeft;
            Top = _settings.WindowTop;
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
        Width = _settings.WindowWidth;
        Height = _settings.WindowHeight;
        Topmost = _settings.AlwaysOnTop;
        Opacity = _settings.WindowOpacity;
        MenuAlwaysOnTop.IsChecked = _settings.AlwaysOnTop;
        // The registry is the source of truth here, not settings.json.
        StartupService.RefreshPathIfEnabled();
        MenuStartWithWindows.IsChecked = StartupService.IsEnabled();
        MenuShowSeconds.IsChecked = _settings.ShowSeconds;
        MenuShowDate.IsChecked = _settings.ShowDate;
        Menu24Hour.IsChecked = _settings.Use24Hour;
        LoadFace(_settings.FaceName);
        UpdateFaceMenuChecks();
    }

    private void LoadFace(string faceName)
    {
        UserControl face = faceName switch
        {
            "AnalogMinimal"  => new AnalogMinimalFace(),
            "AnalogVintage"  => new AnalogVintageFace(),
            "AnalogNeon"     => new AnalogNeonFace(),
            "DigitalLcd"     => new DigitalLcdFace(),
            "DigitalModern"  => new DigitalModernFace(),
            "DigitalNeon"    => new DigitalNeonFace(),
            "DigitalBinary"  => new DigitalBinaryFace(),
            "DigitalWord"    => new DigitalWordFace(),
            "DigitalMatrix"  => new DigitalMatrixFace(),
            _                => new AnalogClassicFace(),
        };
        _currentFace = face as IClockFace;
        FaceHost.Content = face;
        _settings.FaceName = faceName;
        ApplyFaceColors();
        ApplyFaceFonts();
    }

    // ---------------- Fonts ----------------
    // WPF sizes text in DIPs (1/96"), the font picker in points (1/72").
    private const double PointsToDips = 96.0 / 72.0;

    private void ApplyFaceFonts() => _currentFace?.ApplyFonts(_settings.TimeFont, _settings.DateFont);

    private void RefreshFace()
        => _currentFace?.UpdateTime(DateTime.Now, _settings.ShowSeconds, _settings.Use24Hour, _settings.ShowDate);

    private void MenuTimeFont_Click(object sender, RoutedEventArgs e) => PickFont(forTime: true);

    private void MenuDateFont_Click(object sender, RoutedEventArgs e) => PickFont(forTime: false);

    private void PickFont(bool forTime)
    {
        using var dlg = new System.Windows.Forms.FontDialog
        {
            ShowEffects = false,   // underline/strikeout/colour aren't applied here
            FontMustExist = true,
            MinSize = 6,
            MaxSize = 200,
        };

        var current = forTime ? _settings.TimeFont : _settings.DateFont;
        if (current != null)
        {
            var style = System.Drawing.FontStyle.Regular;
            if (current.Bold) style |= System.Drawing.FontStyle.Bold;
            if (current.Italic) style |= System.Drawing.FontStyle.Italic;
            try
            {
                dlg.Font = new System.Drawing.Font(current.Family, (float)(current.Size / PointsToDips), style);
            }
            catch { /* family no longer installed — let the dialog open on its default */ }
        }

        if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

        var choice = new FontChoice
        {
            Family = dlg.Font.Name,
            Size = dlg.Font.SizeInPoints * PointsToDips,
            Bold = dlg.Font.Bold,
            Italic = dlg.Font.Italic,
        };
        if (forTime) _settings.TimeFont = choice; else _settings.DateFont = choice;

        ApplyFaceFonts();
        SaveSettings();
    }

    private void MenuResetFonts_Click(object sender, RoutedEventArgs e)
    {
        _settings.TimeFont = null;
        _settings.DateFont = null;
        ApplyFaceFonts();
        SaveSettings();
    }

    // ---------------- Colours ----------------
    private void ApplyFaceColors()
        => _currentFace?.ApplyColors(
            ParseColor(_settings.TimeColor),
            ParseColor(_settings.DateColor),
            ParseColor(_settings.BackgroundColor),
            _settings.BackgroundOpacity,
            _settings.TextOpacity);

    private static System.Windows.Media.Color? ParseColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;
        try
        {
            return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
        }
        catch
        {
            return null; // fall back to the face's own default
        }
    }

    private enum ColorTarget { Time, Date, Background }

    private void MenuTimeColor_Click(object sender, RoutedEventArgs e) => PickColor(ColorTarget.Time);

    private void MenuDateColor_Click(object sender, RoutedEventArgs e) => PickColor(ColorTarget.Date);

    private void MenuBackgroundColor_Click(object sender, RoutedEventArgs e) => PickColor(ColorTarget.Background);

    private void PickColor(ColorTarget target)
    {
        string? current = target switch
        {
            ColorTarget.Time => _settings.TimeColor,
            ColorTarget.Date => _settings.DateColor,
            _                => _settings.BackgroundColor,
        };

        using var dlg = new System.Windows.Forms.ColorDialog { FullOpen = true, AnyColor = true };
        if (ParseColor(current) is System.Windows.Media.Color c)
            dlg.Color = System.Drawing.Color.FromArgb(c.R, c.G, c.B);

        if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

        string hex = $"#{dlg.Color.R:X2}{dlg.Color.G:X2}{dlg.Color.B:X2}";
        switch (target)
        {
            case ColorTarget.Time: _settings.TimeColor = hex; break;
            case ColorTarget.Date: _settings.DateColor = hex; break;
            default:               _settings.BackgroundColor = hex; break;
        }

        ApplyFaceColors();
        SaveSettings();
    }

    private void MenuResetColors_Click(object sender, RoutedEventArgs e)
    {
        _settings.TimeColor = null;
        _settings.DateColor = null;
        _settings.BackgroundColor = null;
        ApplyFaceColors();
        SaveSettings();
    }

    private void UpdateFaceMenuChecks()
    {
        foreach (MenuItem item in MenuFace.Items)
            item.IsChecked = (string)item.Tag == _settings.FaceName;
    }

    private void SaveSettings()
    {
        _settings.WindowLeft = Left;
        _settings.WindowTop = Top;
        _settings.WindowWidth = Width;
        _settings.WindowHeight = Height;
        _settingsService.Save(_settings);
    }

    protected override void OnClosed(EventArgs e)
    {
        SaveSettings();
        base.OnClosed(e);
    }

    // ---- Borderless-window resize via native hit-testing ----
    private const int WM_NCHITTEST = 0x0084;
    private const int WM_GETMINMAXINFO = 0x0024;
    private const int HTCLIENT = 1;
    private const int HTLEFT = 10, HTRIGHT = 11, HTTOP = 12, HTTOPLEFT = 13,
                      HTTOPRIGHT = 14, HTBOTTOM = 15, HTBOTTOMLEFT = 16, HTBOTTOMRIGHT = 17;
    private const double ResizeBorder = 10.0; // grab thickness in DIPs

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        // Lift the default max-resize cap (~monitor size) so the widget can be
        // dragged much larger, up to the full virtual desktop across monitors.
        if (msg == WM_GETMINMAXINFO)
        {
            var mmi = Marshal.PtrToStructure<NativeMethods.MINMAXINFO>(lParam);
            int vw = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN);
            int vh = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN);
            if (vw > 0 && vh > 0)
            {
                mmi.ptMaxTrackSize.X = vw;
                mmi.ptMaxTrackSize.Y = vh;
            }
            double minDpi = HwndSource.FromHwnd(hwnd)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            mmi.ptMinTrackSize.X = (int)(MinWidth * minDpi);
            mmi.ptMinTrackSize.Y = (int)(MinHeight * minDpi);
            Marshal.StructureToPtr(mmi, lParam, false);
            handled = true;
            return IntPtr.Zero;
        }

        if (msg != WM_NCHITTEST) return IntPtr.Zero;

        // Mouse position: signed physical screen coords packed in lParam (low=x, high=y).
        long lp = lParam.ToInt64();
        int mx = (short)(lp & 0xFFFF);
        int my = (short)((lp >> 16) & 0xFFFF);

        // Compare against the real window rectangle, in the same physical-pixel space.
        if (!NativeMethods.GetWindowRect(hwnd, out var r)) return IntPtr.Zero;

        // Convert the DIP grab thickness to physical pixels for the current DPI.
        double dpi = HwndSource.FromHwnd(hwnd)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        int b = (int)Math.Ceiling(ResizeBorder * dpi);

        bool left = mx <= r.Left + b, right = mx >= r.Right - b;
        bool top = my <= r.Top + b, bottom = my >= r.Bottom - b;

        int hit =
            top && left ? HTTOPLEFT :
            top && right ? HTTOPRIGHT :
            bottom && left ? HTBOTTOMLEFT :
            bottom && right ? HTBOTTOMRIGHT :
            left ? HTLEFT :
            right ? HTRIGHT :
            top ? HTTOP :
            bottom ? HTBOTTOM :
            HTCLIENT;

        if (hit == HTCLIENT) return IntPtr.Zero; // let WPF handle the body (drag/context menu)
        handled = true;
        return (IntPtr)hit;
    }

    protected override void OnLocationChanged(EventArgs e) { SaveSettings(); base.OnLocationChanged(e); }
    protected override void OnRenderSizeChanged(SizeChangedInfo info) { SaveSettings(); base.OnRenderSizeChanged(info); }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void FaceMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var item = (MenuItem)sender;
        LoadFace((string)item.Tag);
        UpdateFaceMenuChecks();
        SaveSettings();
        RefreshFace();
    }

    private void MenuAlwaysOnTop_Click(object sender, RoutedEventArgs e)
    {
        _settings.AlwaysOnTop = MenuAlwaysOnTop.IsChecked;
        Topmost = _settings.AlwaysOnTop;
        SaveSettings();
    }

    private void MenuStartWithWindows_Click(object sender, RoutedEventArgs e)
    {
        StartupService.SetEnabled(MenuStartWithWindows.IsChecked);
        // Reflect what actually stuck, in case the write was refused.
        MenuStartWithWindows.IsChecked = StartupService.IsEnabled();
    }

    private void MenuShowSeconds_Click(object sender, RoutedEventArgs e)
    {
        _settings.ShowSeconds = MenuShowSeconds.IsChecked;
        SaveSettings();
        RefreshFace();
    }

    private void MenuShowDate_Click(object sender, RoutedEventArgs e)
    {
        _settings.ShowDate = MenuShowDate.IsChecked;
        SaveSettings();
        RefreshFace();
    }

    private void Menu24Hour_Click(object sender, RoutedEventArgs e)
    {
        _settings.Use24Hour = Menu24Hour.IsChecked;
        SaveSettings();
        RefreshFace();
    }

    private void OpacityMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var item = (MenuItem)sender;
        _settings.WindowOpacity = double.Parse((string)item.Tag, System.Globalization.CultureInfo.InvariantCulture);
        Opacity = _settings.WindowOpacity;
        SaveSettings();
    }

    private void BackgroundOpacityMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var item = (MenuItem)sender;
        _settings.BackgroundOpacity = double.Parse((string)item.Tag, System.Globalization.CultureInfo.InvariantCulture);
        ApplyFaceColors();
        SaveSettings();
    }

    private void TextOpacityMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var item = (MenuItem)sender;
        _settings.TextOpacity = double.Parse((string)item.Tag, System.Globalization.CultureInfo.InvariantCulture);
        ApplyFaceColors();
        SaveSettings();
    }

    private void ResizeGrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            e.Handled = true;
            var helper = new WindowInteropHelper(this);
            NativeMethods.SendMessage(helper.Handle, 0x112, (IntPtr)0xF008, IntPtr.Zero);
        }
    }

    // ---------------- Alarms ----------------
    private void CheckAlarms(DateTime now)
    {
        if (now <= _lastAlarmCheck) { _lastAlarmCheck = now; return; }

        bool changed = false;

        foreach (var alarm in _settings.Alarms)
        {
            if (!alarm.Enabled) continue;
            if (alarm.Hour is < 0 or > 23 || alarm.Minute is < 0 or > 59) continue;
            var occ = new DateTime(now.Year, now.Month, now.Day, alarm.Hour, alarm.Minute, 0);
            if (occ > _lastAlarmCheck && occ <= now)
            {
                FireAlarm(alarm.Name, FormatAlarmTime(alarm.Hour, alarm.Minute));
                if (!alarm.Repeat) { alarm.Enabled = false; changed = true; }
            }
        }

        for (int i = _snoozes.Count - 1; i >= 0; i--)
        {
            if (_snoozes[i].When > _lastAlarmCheck && _snoozes[i].When <= now)
            {
                FireAlarm(_snoozes[i].Name, FormatAlarmTime(now.Hour, now.Minute));
                _snoozes.RemoveAt(i);
            }
        }

        _lastAlarmCheck = now;
        if (changed) SaveSettings();
    }

    private void FireAlarm(string name, string timeText)
    {
        var safeName = string.IsNullOrWhiteSpace(name) ? "Alarm" : name;
        var ring = new AlarmRingWindow(safeName, timeText);
        ring.SnoozeRequested += minutes => _snoozes.Add((DateTime.Now.AddMinutes(minutes), safeName));
        ring.Show();
    }

    private string FormatAlarmTime(int hour, int minute)
    {
        var dt = new DateTime(2000, 1, 1, hour, minute, 0);
        return dt.ToString(_settings.Use24Hour ? "HH:mm" : "h:mm tt");
    }

    // ---------------- Tools window ----------------
    private void OpenTools(string tab)
    {
        if (_toolsWindow == null)
        {
            _toolsWindow = new ToolsWindow(_settings, SaveSettings);
            _toolsWindow.Closed += (_, _) => _toolsWindow = null;
            _toolsWindow.Show();
        }
        else
        {
            if (_toolsWindow.WindowState == WindowState.Minimized)
                _toolsWindow.WindowState = WindowState.Normal;
            _toolsWindow.Activate();
        }
        _toolsWindow.ShowTab(tab);
    }

    private void MenuAlarms_Click(object sender, RoutedEventArgs e) => OpenTools("Alarms");
    private void MenuStopwatch_Click(object sender, RoutedEventArgs e) => OpenTools("Stopwatch");
    private void MenuTimer_Click(object sender, RoutedEventArgs e) => OpenTools("Timer");

    private void MenuExit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

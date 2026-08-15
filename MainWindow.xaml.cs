using System.Reflection;
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
    private readonly GeoService _geo = new();
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
        _currentFace?.UpdateTime(MainNow(), _settings.ShowSeconds, _settings.Use24Hour, _settings.ShowDate);
        UpdateLocationTimes(DateTime.UtcNow);
        ReassertTopmost();
        // Alarms are wall-clock times on this PC, so they stay on local time even when
        // the big clock is showing somewhere else.
        CheckAlarms(DateTime.Now);
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
        MenuAbout.Header = $"About - V.{AppVersion}";
        RebuildLocationStrip();
        BuildLocationMenu();
    }

    /// <summary>The assembly's version, as set by &lt;Version&gt; in the csproj.</summary>
    private static string AppVersion
    {
        get
        {
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            string? informational = asm
                .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

            // The SDK can append "+<commit hash>" to the informational version.
            if (!string.IsNullOrWhiteSpace(informational))
                return informational.Split('+')[0];

            return asm.GetName().Version?.ToString(2) ?? "1.0";
        }
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

    private void ApplyFaceFonts()
    {
        _currentFace?.ApplyFonts(_settings.TimeFont, _settings.DateFont);
        StyleLocationStrip();
    }

    private void RefreshFace()
        => _currentFace?.UpdateTime(MainNow(), _settings.ShowSeconds, _settings.Use24Hour, _settings.ShowDate);

    // ---------------- Location strip ----------------
    // The time TextBlock for each configured city, paired with the zone it reads.
    private readonly List<(TextBlock Time, TimeZoneInfo Zone)> _locationRows = new();

    /// <summary>
    /// The Location menu is built from the cities themselves: each configured city is a
    /// tick box carrying its own name. StaysOpenOnClick keeps the menu up so several can
    /// be toggled in one visit.
    /// </summary>
    private void BuildLocationMenu()
    {
        MenuLocation.Items.Clear();

        foreach (var (city, index) in _settings.Locations.Select((c, i) => (c, i)))
        {
            var toggle = new MenuItem
            {
                Header = city.Name,
                IsCheckable = true,
                IsChecked = city.Visible,
                StaysOpenOnClick = true,
                Tag = index,
            };
            toggle.Click += CityVisibility_Click;
            MenuLocation.Items.Add(toggle);
        }

        if (_settings.Locations.Count > 0) MenuLocation.Items.Add(new Separator());

        for (int slot = 0; slot < ClockSettings.MaxLocations; slot++)
        {
            var edit = new MenuItem
            {
                Header = slot < _settings.Locations.Count
                    ? $"Change {_settings.Locations[slot].Name}…"
                    : $"Set City {slot + 1}…",
                Tag = slot,
            };
            edit.Click += MenuLocationSlot_Click;
            MenuLocation.Items.Add(edit);
        }

        MenuLocation.Items.Add(new Separator());

        var main = new MenuItem { Header = $"Main Clock: {MainLocationLabel()}…" };
        main.Click += MenuMainLocation_Click;
        MenuLocation.Items.Add(main);

        if (_settings.Locations.Count > 0)
        {
            var clear = new MenuItem { Header = "Clear All Cities" };
            clear.Click += MenuClearLocations_Click;
            MenuLocation.Items.Add(clear);
        }
    }

    private void CityVisibility_Click(object sender, RoutedEventArgs e)
    {
        var item = (MenuItem)sender;
        int index = (int)item.Tag;
        if (index >= _settings.Locations.Count) return;

        _settings.Locations[index].Visible = item.IsChecked;
        RebuildLocationStrip();
        SaveSettings();
        // Deliberately not rebuilding the menu here — that would tear down the very
        // item being clicked and close the menu we are keeping open.
    }

    private void MenuLocationSlot_Click(object sender, RoutedEventArgs e)
    {
        int slot = (int)((MenuItem)sender).Tag;
        CityClock? existing = slot < _settings.Locations.Count ? _settings.Locations[slot] : null;

        var dlg = new LocationPickerWindow($"CITY {slot + 1}", existing, _geo) { Owner = this };
        if (dlg.ShowDialog() != true) return;

        if (dlg.Removed)
        {
            if (slot < _settings.Locations.Count) _settings.Locations.RemoveAt(slot);
        }
        else if (dlg.Result is CityClock city)
        {
            if (slot < _settings.Locations.Count)
            {
                city.Visible = _settings.Locations[slot].Visible;
                _settings.Locations[slot] = city;
            }
            else if (_settings.Locations.Count < ClockSettings.MaxLocations)
            {
                _settings.Locations.Add(city);
            }
        }

        RebuildLocationStrip();
        BuildLocationMenu();
        SaveSettings();
    }

    private void MenuMainLocation_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new LocationPickerWindow("MAIN CLOCK", _settings.MainLocation, _geo) { Owner = this };
        if (dlg.ShowDialog() != true) return;

        // Removing the main location hands the big clock back to this PC's own time.
        _settings.MainLocation = dlg.Removed ? null : dlg.Result;

        RebuildLocationStrip();
        BuildLocationMenu();
        RefreshFace();
        SaveSettings();
    }

    private void MenuClearLocations_Click(object sender, RoutedEventArgs e)
    {
        _settings.Locations.Clear();
        RebuildLocationStrip();
        BuildLocationMenu();
        SaveSettings();
    }

    // ---------------- Main clock's own place ----------------

    /// <summary>The time the big clock shows: a chosen city's, or this PC's.</summary>
    private DateTime MainNow()
        => _settings.MainLocation is CityClock city
            ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZones.Resolve(city.TimeZoneId))
            : DateTime.Now;

    /// <summary>
    /// What to call the big clock's place. A city the user picked wins; otherwise the
    /// machine's own zone names it — "America/Denver" reads as "DENVER".
    /// </summary>
    private string MainLocationLabel()
    {
        if (_settings.MainLocation is CityClock city && city.Name.Length > 0) return city.Name;

        string id = TimeZoneInfo.Local.Id;
        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(id, out string? iana) && iana is not null) id = iana;

        int slash = id.LastIndexOf('/');
        string place = slash >= 0 ? id[(slash + 1)..] : id;
        return place.Replace('_', ' ').ToUpperInvariant();
    }

    private void RebuildLocationStrip()
    {
        _locationRows.Clear();
        LocationRows.Children.Clear();

        var cities = _settings.Locations
            .Take(ClockSettings.MaxLocations)
            .Where(c => c.Visible)
            .ToList();

        // The big clock is only worth labelling once there is another place to confuse
        // it with, or when it is showing somewhere other than this PC.
        bool labelMain = cities.Count > 0 || _settings.MainLocation != null;
        MainLabelText.Text = MainLocationLabel();
        MainLabelText.Visibility = labelMain ? Visibility.Visible : Visibility.Collapsed;
        MainLabelRow.Height = labelMain ? GridLength.Auto : new GridLength(0);

        if (cities.Count == 0)
        {
            LocationStrip.Visibility = Visibility.Collapsed;
            LocationRow.Height = new GridLength(0);
            StyleLocationStrip();
            return;
        }

        LocationStrip.Visibility = Visibility.Visible;
        // One star per city, against the face's two, so the strip grows with the list
        // instead of squeezing the clock to nothing.
        LocationRow.Height = new GridLength(cities.Count, GridUnitType.Star);

        foreach (var city in cities)
        {
            var row = new Grid { Margin = new Thickness(0, 1, 0, 1) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var name = new TextBlock
            {
                Text = city.Name,
                FontSize = 13,
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            var time = new TextBlock
            {
                FontSize = 15,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            Grid.SetColumn(name, 0);
            Grid.SetColumn(time, 1);
            row.Children.Add(name);
            row.Children.Add(time);
            LocationRows.Children.Add(row);

            _locationRows.Add((time, ResolveZone(city.TimeZoneId)));
        }

        StyleLocationStrip();
        UpdateLocationTimes(DateTime.UtcNow);
    }

    // Zone ids are stored in IANA form; TimeZones maps them onto whatever Windows calls them.
    private static TimeZoneInfo ResolveZone(string id) => TimeZones.Resolve(id);

    private void UpdateLocationTimes(DateTime utcNow)
    {
        string fmt = _settings.Use24Hour
            ? (_settings.ShowSeconds ? "HH:mm:ss" : "HH:mm")
            : (_settings.ShowSeconds ? "h:mm:ss tt" : "h:mm tt");

        foreach (var (text, zone) in _locationRows)
            text.Text = TimeZoneInfo.ConvertTimeFromUtc(utcNow, zone).ToString(fmt);
    }

    /// <summary>Keeps the strip in step with the colour, font and opacity menus.</summary>
    private void StyleLocationStrip()
    {
        LocationStrip.Background = Faces.FaceBrush.Background(
            ParseColor(_settings.BackgroundColor),
            System.Windows.Media.Brushes.Black,
            _settings.BackgroundOpacity);

        var nameBrush = ParseColor(_settings.DateColor) is System.Windows.Media.Color d
            ? new System.Windows.Media.SolidColorBrush(d)
            : System.Windows.Media.Brushes.LightGray;
        var timeBrush = ParseColor(_settings.TimeColor) is System.Windows.Media.Color t
            ? new System.Windows.Media.SolidColorBrush(t)
            : System.Windows.Media.Brushes.White;

        MainLabelText.Foreground = ParseColor(_settings.DateColor) is System.Windows.Media.Color m
            ? new System.Windows.Media.SolidColorBrush(m)
            : System.Windows.Media.Brushes.LightGray;
        MainLabelText.Opacity = _settings.TextOpacity;
        if (_settings.DateFont is FontChoice mf)
            MainLabelText.FontFamily = new System.Windows.Media.FontFamily(mf.Family);

        foreach (Grid row in LocationRows.Children.OfType<Grid>())
        {
            if (row.Children.Count < 2) continue;
            var name = (TextBlock)row.Children[0];
            var time = (TextBlock)row.Children[1];

            name.Foreground = nameBrush;
            time.Foreground = timeBrush;
            name.Opacity = time.Opacity = _settings.TextOpacity;

            if (_settings.DateFont is FontChoice df) name.FontFamily = new System.Windows.Media.FontFamily(df.Family);
            if (_settings.TimeFont is FontChoice tf) time.FontFamily = new System.Windows.Media.FontFamily(tf.Family);
        }
    }

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
    {
        _currentFace?.ApplyColors(
            ParseColor(_settings.TimeColor),
            ParseColor(_settings.DateColor),
            ParseColor(_settings.BackgroundColor),
            _settings.BackgroundOpacity,
            _settings.TextOpacity);
        StyleLocationStrip();
    }

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

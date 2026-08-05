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

    public MainWindow()
    {
        InitializeComponent();
        _settings = _settingsService.Load();
        ApplySettings();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) => _currentFace?.UpdateTime(DateTime.Now, _settings.ShowSeconds, _settings.Use24Hour);
        _timer.Start();
        Loaded += (_, _) => _currentFace?.UpdateTime(DateTime.Now, _settings.ShowSeconds, _settings.Use24Hour);
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
        MenuShowSeconds.IsChecked = _settings.ShowSeconds;
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
        _currentFace?.UpdateTime(DateTime.Now, _settings.ShowSeconds, _settings.Use24Hour);
    }

    private void MenuAlwaysOnTop_Click(object sender, RoutedEventArgs e)
    {
        _settings.AlwaysOnTop = MenuAlwaysOnTop.IsChecked;
        Topmost = _settings.AlwaysOnTop;
        SaveSettings();
    }

    private void MenuShowSeconds_Click(object sender, RoutedEventArgs e)
    {
        _settings.ShowSeconds = MenuShowSeconds.IsChecked;
        SaveSettings();
        _currentFace?.UpdateTime(DateTime.Now, _settings.ShowSeconds, _settings.Use24Hour);
    }

    private void Menu24Hour_Click(object sender, RoutedEventArgs e)
    {
        _settings.Use24Hour = Menu24Hour.IsChecked;
        SaveSettings();
        _currentFace?.UpdateTime(DateTime.Now, _settings.ShowSeconds, _settings.Use24Hour);
    }

    private void OpacityMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var item = (MenuItem)sender;
        _settings.WindowOpacity = double.Parse((string)item.Tag, System.Globalization.CultureInfo.InvariantCulture);
        Opacity = _settings.WindowOpacity;
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

    private void MenuExit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

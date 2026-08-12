using Microsoft.Win32;

namespace ClockWidg.Services;

/// <summary>
/// Registers the widget under the current user's Run key so it starts with Windows.
/// Per-user (HKCU) rather than machine-wide, so it needs no elevation.
/// </summary>
public static class StartupService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ClockWidg";

    /// <summary>The single-file exe's own path, or null if it can't be determined.</summary>
    private static string? ExePath => Environment.ProcessPath;

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) != null;
        }
        catch
        {
            return false;
        }
    }

    public static void SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (key == null) return;

            if (enabled)
            {
                if (ExePath is not string path) return;
                key.SetValue(ValueName, $"\"{path}\"");
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch { /* a locked-down or roaming profile just means the box won't stick */ }
    }

    /// <summary>
    /// Rewrites the registered command if the exe has since moved, so an enabled
    /// entry doesn't quietly point at a path that no longer exists.
    /// </summary>
    public static void RefreshPathIfEnabled()
    {
        try
        {
            if (ExePath is not string path) return;
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (key?.GetValue(ValueName) is not string existing) return;

            string wanted = $"\"{path}\"";
            if (!string.Equals(existing, wanted, StringComparison.OrdinalIgnoreCase))
                key.SetValue(ValueName, wanted);
        }
        catch { }
    }
}

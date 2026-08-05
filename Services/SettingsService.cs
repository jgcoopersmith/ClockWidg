using System.IO;
using System.Text.Json;
using ClockWidg.Models;

namespace ClockWidg.Services;

public class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ClockWidg", "settings.json");

    public ClockSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
                return JsonSerializer.Deserialize<ClockSettings>(File.ReadAllText(SettingsPath)) ?? new ClockSettings();
        }
        catch { }
        return new ClockSettings();
    }

    public void Save(ClockSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}

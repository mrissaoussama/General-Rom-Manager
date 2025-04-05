// AvaloniaApplication1/AvaloniaUI/Utils/Settings.cs

using System.Text.Json;

namespace AvaloniaUI.Utils;

public class Settings
{
    private static readonly string _settingsPath = "settings.json";
    public Dictionary<string, List<string>> VisibleColumns { get; set; } = new();

    public static Settings Instance { get; private set; } = Load();

    public static Settings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
                Instance = settings;
                return settings;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading settings: {ex.Message}");
        }

        Instance = new Settings();
        return Instance;
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving settings: {ex.Message}");
        }
    }
}
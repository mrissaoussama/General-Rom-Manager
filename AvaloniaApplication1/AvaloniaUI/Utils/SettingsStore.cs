using System.Text.Json;

namespace AvaloniaUI.Utils;

/// <summary>Loads and saves <see cref="Settings"/>.</summary>
public interface ISettingsStore
{
    Settings Current { get; }
    string Path { get; }
    Settings Reload();
    void Save(Settings settings);
}

/// <summary>
/// settings.json. Saves are atomic (written to a temp file, then swapped in), so a crash never leaves a
/// half-written file. A file that cannot be read is kept as <c>settings.corrupt-&lt;time&gt;.json</c> before
/// defaults are used, instead of being overwritten by the next save.
/// </summary>
public sealed class JsonSettingsStore(string path) : ISettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly object _lock = new();
    private Settings? _current;

    public string Path { get; } = path;

    public Settings Current
    {
        get
        {
            lock (_lock) return _current ??= Read();
        }
    }

    /// <summary>
    /// settings.json next to the executable when one is there (portable mode); otherwise the per-user
    /// app data folder, which survives updates that replace the program folder.
    /// </summary>
    public static string DefaultPath()
    {
        var portable = System.IO.Path.Combine(AppContext.BaseDirectory, "settings.json");
        if (File.Exists(portable)) return portable;
        return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GeneralRomManager", "settings.json");
    }

    public Settings Reload()
    {
        lock (_lock) return _current = Read();
    }

    public void Save(Settings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, Options);
            lock (_lock)
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
                var tmp = Path + ".tmp";
                File.WriteAllText(tmp, json);
                File.Move(tmp, Path, overwrite: true);
            }
        }
        catch (Exception ex)
        {
            AppLog.Error($"Saving settings to {Path} failed: {ex.Message}");
        }
    }

    private Settings Read()
    {
        if (!File.Exists(Path)) return new Settings();
        try
        {
            return JsonSerializer.Deserialize<Settings>(File.ReadAllText(Path)) ?? new Settings();
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            var backup = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path)!, $"settings.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            try { File.Copy(Path, backup, overwrite: true); } catch (IOException) { }
            AppLog.Error($"settings.json could not be read ({ex.Message}); kept a copy at {backup} and started with defaults.");
            return new Settings();
        }
        catch (IOException ex)
        {
            AppLog.Error($"settings.json could not be opened: {ex.Message}");
            return new Settings();
        }
    }
}

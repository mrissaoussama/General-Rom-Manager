using System.Security.Cryptography;
using System.Text.Json;
using RomManagerShared.Configuration;

namespace RomManagerShared.Utils;

public enum ToolPinStatus
{
    /// <summary>Never run before: pinned on first use.</summary>
    New,
    /// <summary>Same file as the last approved run.</summary>
    Unchanged,
    /// <summary>The program at this path is not the one that ran before.</summary>
    Changed,
}

/// <summary>
/// The SHA-256 of each user-supplied program (ADR 0002), keyed by its full path. A program that changes after it
/// first ran (an update, or something replaced it) needs the user's confirmation before it runs again.
/// </summary>
public sealed class ToolPins(string path)
{
    private readonly object _lock = new();
    private Dictionary<string, string>? _pins;

    private static ToolPins? _default;

    /// <summary><c>tool-pins.json</c> next to the database (it survives clearing the cache); null when unconfigured.</summary>
    public static ToolPins? Default
    {
        get
        {
            if (_default is not null) return _default;
            var db = RomManagerConfiguration.GetSqliteDBPath();
            return string.IsNullOrEmpty(db) ? null
                : _default = new ToolPins(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(db))!, "tool-pins.json"));
        }
    }

    public ToolPinStatus Check(string exePath)
    {
        var hash = HashFile(exePath);
        lock (_lock)
            return !Load().TryGetValue(Key(exePath), out var pinned) ? ToolPinStatus.New
                : pinned.Equals(hash, StringComparison.OrdinalIgnoreCase) ? ToolPinStatus.Unchanged
                : ToolPinStatus.Changed;
    }

    /// <summary>Records the program as it is now.</summary>
    public void Pin(string exePath)
    {
        var hash = HashFile(exePath);
        lock (_lock)
        {
            Load()[Key(exePath)] = hash;
            Save();
        }
    }

    private static string Key(string exePath) => Path.GetFullPath(exePath);

    private static string HashFile(string exePath)
    {
        using var stream = FileUtils.OpenRead(exePath);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private Dictionary<string, string> Load()
    {
        if (_pins is not null) return _pins;
        try
        {
            _pins = File.Exists(path)
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? []
                : [];
        }
        catch (Exception ex)
        {
            AppLog.Warn($"[ToolPins] {path}: {ex.Message}");
            _pins = [];
        }
        // Keys compare like the OS compares paths.
        return _pins = new Dictionary<string, string>(_pins, FileUtils.PathComparer);
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(_pins, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(tmp, path, overwrite: true);
        }
        catch (Exception ex) { AppLog.Warn($"[ToolPins] save failed: {ex.Message}"); }
    }
}

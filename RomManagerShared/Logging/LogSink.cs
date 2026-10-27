using Microsoft.Extensions.Logging;
using RomManagerShared.Configuration;
using RomManagerShared.Utils;

namespace RomManagerShared.Logging;

/// <summary>
/// Where log entries end up: the log file (serialised: parsers log in parallel), the last 500 entries for the
/// log panel, <see cref="LogAdded"/>, and the "not recognised" reason of the file being parsed (<see cref="ParseDiagnostics"/>).
/// </summary>
public static class LogSink
{
    private static readonly List<string> Recent = [];
    private static readonly object FileLock = new();

    /// <summary>Fired on the logging thread for every entry.</summary>
    public static event Action<string>? LogAdded;

    /// <summary>Tests point the log somewhere private.</summary>
    internal static string? FilePathOverride { get; set; }

    /// <summary>config.json's ErrorLogs, or a per-user fallback when the config has none.</summary>
    public static string FilePath
    {
        get
        {
            if (FilePathOverride is { } path) return path;
            try
            {
                var configured = RomManagerConfiguration.GetErrorLogPath();
                if (!string.IsNullOrWhiteSpace(configured)) return configured;
            }
            catch { /* fall through */ }
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GeneralRomManager", "Logs", "errors.log");
        }
    }

    public static IReadOnlyList<string> GetRecent()
    {
        lock (Recent) return [.. Recent];
    }

    public static void Write(LogLevel level, string category, string message, Exception? exception = null)
    {
        ParseDiagnostics.Add(message);
        var tag = level switch
        {
            LogLevel.Warning => "WARN ",
            LogLevel.Error or LogLevel.Critical => "ERROR ",
            _ => "",
        };
        var entry = $"[{DateTime.Now:HH:mm:ss}] {tag}[{category}] {message}" + (exception is null ? "" : Environment.NewLine + exception);
        lock (Recent)
        {
            Recent.Add(entry);
            if (Recent.Count > 500) Recent.RemoveAt(0);
        }
        LogAdded?.Invoke(entry);
        try
        {
            var path = FilePath;
            lock (FileLock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.AppendAllText(path, entry + Environment.NewLine);
            }
        }
        catch { /* logging must never crash the app */ }
    }
}

using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace RomManagerShared.Configuration;

/// <summary>
/// Loads config.json and config.local.json and exposes them as <see cref="AppOptions"/>. The app also registers
/// them as <c>IOptionsMonitor&lt;AppOptions&gt;</c>; this static accessor serves code that is not built by DI
/// (parsers' static helpers, modules' configuration classes). Edits to the files are picked up while running,
/// except <see cref="GlobalPathsOptions.BaseDirectory"/>, which is fixed at start (the database lives there).
/// </summary>
public static class RomManagerConfiguration
{
    private static AppOptions? _options;
    private static readonly ConcurrentDictionary<(string, Type), object> _consoleOptions = new();

    public static IConfigurationRoot Configuration { get; private set; } = new ConfigurationBuilder().Build();
    public static bool ConfigurationLoaded { get; private set; }
    public static string BaseFolder { get; private set; } = "";

    /// <summary>Current options (rebound when the files change).</summary>
    public static AppOptions Options => _options ??= Configuration.Get<AppOptions>() ?? new AppOptions();

    public static void Load(string jsonConfigPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(jsonConfigPath);

        // The app folder, so the config is found whatever the working directory ('dotnet run' from the repo root).
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(jsonConfigPath, optional: true, reloadOnChange: true)
            // Personal overrides (base folder, ROM folders) that stay out of git and releases.
            .AddJsonFile(Path.ChangeExtension(jsonConfigPath, ".local.json"), optional: true, reloadOnChange: true)
            .Build();
        Reset();
        ChangeToken.OnChange(Configuration.GetReloadToken, Reset);

        BaseFolder = GetBaseFolderPath();
        try { Directory.CreateDirectory(BaseFolder); }
        catch { /* best-effort; if AppData is inaccessible, later operations will surface a meaningful error */ }

        ConfigurationLoaded = true;
    }

    private static void Reset()
    {
        _options = null;
        _consoleOptions.Clear();
    }

    /// <summary>
    /// A console module's section <c>Consoles:&lt;consoleId&gt;</c> bound to <typeparamref name="T"/>; defaults
    /// (the property initialisers of <typeparamref name="T"/>) fill whatever the files leave out.
    /// </summary>
    public static T ForConsole<T>(string consoleId) where T : class, new() =>
        (T)_consoleOptions.GetOrAdd((consoleId, typeof(T)),
            key => Configuration.GetSection($"Consoles:{key.Item1}").Get<T>() ?? new T());

    public static string GetBaseFolderPath()
    {
        // Honour the explicit BaseDirectory so users can point to any writable drive.
        var configured = Options.GlobalPaths.BaseDirectory;
        return !string.IsNullOrWhiteSpace(configured)
            ? Environment.ExpandEnvironmentVariables(configured)
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GeneralRomManager");
    }

    /// <summary><paramref name="relative"/> under the base folder (absolute paths stay as they are).</summary>
    public static string UnderBase(string relative) =>
        Path.IsPathFullyQualified(relative) ? relative : Path.Combine(BaseFolder, relative.TrimStart('\\', '/'));

    /// <summary>Base-relative <paramref name="value"/>, or <paramref name="fallback"/> when it is empty.</summary>
    public static string UnderBase(string? value, string fallback) =>
        UnderBase(string.IsNullOrWhiteSpace(value) ? fallback : value);

    // Empty until Load: callers skip writing (icons, logs) rather than write next to the working directory.
    public static string GetSqliteDBPath() => Loaded(Options.GlobalPaths.Database);
    public static string GetErrorLogPath() => Loaded(Options.GlobalPaths.ErrorLogs);
    public static string GetPluginsPath() => Loaded(Options.GlobalPaths.Plugins);
    public static string GetCachePath() => Loaded(Options.GlobalPaths.Cache);
    private static string Loaded(string relative) => ConfigurationLoaded ? UnderBase(relative) : string.Empty;

    public static string GetDatsPath() => UnderBase(Options.GlobalPaths.Dats);
    public static string GetModulesPath() => UnderBase(Options.GlobalPaths.Modules);

    /// <summary>ROM folder for a console from <c>Consoles:&lt;id&gt;:RomPath</c> (settings.json wins in the app).</summary>
    public static string? GetConsoleRomPath(string consoleId)
    {
        var path = Configuration.GetSection($"Consoles:{consoleId}")["RomPath"];
        return string.IsNullOrWhiteSpace(path) ? null : path;
    }
}

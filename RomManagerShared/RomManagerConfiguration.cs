using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace RomManagerShared.Configuration;

public static class RomManagerConfiguration
{
    #region Properties
    public static IConfigurationRoot Configuration { get; private set; }
    public static bool ConfigurationLoaded { get; private set; }
    public static string BaseFolder { get; private set; }
    #endregion

    #region Initialization
    public static void Load(string jsonConfigPath)
    {
        if (string.IsNullOrEmpty(jsonConfigPath))
            throw new ArgumentNullException(nameof(jsonConfigPath));

        // Use the assembly directory so the config is found regardless of the
        // working directory (e.g. when running via 'dotnet run' from repo root).
        var basePath = AppContext.BaseDirectory;

        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile(jsonConfigPath, optional: true);

        Configuration = builder.Build();
        BaseFolder = GetBaseFolderPath();

        if (!string.IsNullOrEmpty(BaseFolder))
        {
            try { Directory.CreateDirectory(BaseFolder); }
            catch { /* best-effort; if AppData is inaccessible, later operations will surface a meaningful error */ }
        }

        ConfigurationLoaded = true;
    }
    #endregion

    #region Path Configuration
    public static string GetBaseFolderPath()
    {
        // Honour the explicit BaseDirectory in config so users can point to any writable drive.
        // Falls back to %AppData%\GeneralRomManager only when config is absent or the key is missing.
        var configured = Configuration?.GetSection("GlobalPaths")["BaseDirectory"];
        if (!string.IsNullOrWhiteSpace(configured))
            return Environment.ExpandEnvironmentVariables(configured);

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GeneralRomManager"
        );
    }

    /// <summary>Configured path under the base folder, or <paramref name="fallback"/> when the key is unset.</summary>
    public static string GetPathOrDefault(string key, string fallback)
    {
        var value = Configuration?.GetSection(key).Value;
        if (string.IsNullOrWhiteSpace(value)) value = fallback;
        return Path.Combine(BaseFolder ?? string.Empty, value.TrimStart('\\', '/'));
    }

    public static string GetGlobalPath(string key)
    {
        if (!ConfigurationLoaded) return string.Empty;
        var value = Configuration?.GetSection("GlobalPaths")[key];
        if (value is null) return string.Empty;
        return Path.Combine(BaseFolder, value);
    }

    public static string GetConsoleRomPath(string consoleName)
    {
        if (!ConfigurationLoaded) return string.Empty;
        return Configuration?.GetSection($"Consoles:{consoleName}:RomPath")?.Value ?? string.Empty;
    }

    public static string GetConsoleToolPath(string consoleName, string toolName)
    {
        if (!ConfigurationLoaded) return string.Empty;
        var value = Configuration?.GetSection($"Consoles:{consoleName}:Tools:{toolName}")?.Value;
        if (value is null) return string.Empty;
        return Path.Combine(BaseFolder, value);
    }
    #endregion

    #region Specific Path Accessors
    public static string GetSqliteDBPath()
    {
        return GetGlobalPath("Database");
    }

    public static string GetErrorLogPath()
    {
        return GetGlobalPath("ErrorLogs");
    }

    public static string GetPluginsPath()
    {
        return GetGlobalPath("Plugins");
    }

    public static string GetCachePath()
    {
        return GetGlobalPath("Cache");
    }
    #endregion

    #region Console-Specific Configuration
    public static string GetTitleDBPath(string consoleName)
    {
        if (!ConfigurationLoaded) return string.Empty;
        var value = Configuration?.GetSection($"Consoles:{consoleName}:TitleDB:Path")?.Value;
        if (value is null) return string.Empty;
        return Path.Combine(BaseFolder, value);
    }

    public static string GetTitleDBVersionsFile(string consoleName)
    {
        var dbPath = GetTitleDBPath(consoleName);
        if (string.IsNullOrEmpty(dbPath)) return string.Empty;
        var value = Configuration?.GetSection($"Consoles:{consoleName}:TitleDB:VersionsFile")?.Value;
        return value is null ? string.Empty : Path.Combine(dbPath, value);
    }
    #endregion
}
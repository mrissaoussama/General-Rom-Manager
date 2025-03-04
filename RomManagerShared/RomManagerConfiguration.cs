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

        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(jsonConfigPath, optional: false);

        Configuration = builder.Build();
        BaseFolder = GetBaseFolderPath();

        if (!string.IsNullOrEmpty(BaseFolder))
        {
            Directory.CreateDirectory(BaseFolder);
        }

        ConfigurationLoaded = true;
    }
    #endregion

    #region Path Configuration
    public static string GetBaseFolderPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GeneralRomManager"
        );
    }

    public static string GetGlobalPath(string key)
    {
        return Path.Combine(
            BaseFolder,
            Configuration.GetSection("GlobalPaths")[key] ?? throw new InvalidOperationException($"Global path {key} not found")
        );
    }

    public static string GetConsoleRomPath(string consoleName)
    {
        return Configuration.GetSection($"Consoles:{consoleName}:RomPath")?.Value
            ?? throw new InvalidOperationException($"Rom path for {consoleName} not found");
    }

    public static string GetConsoleToolPath(string consoleName, string toolName)
    {
        return Path.Combine(
            BaseFolder,
            Configuration.GetSection($"Consoles:{consoleName}:Tools:{toolName}")?.Value
                ?? throw new InvalidOperationException($"Tool {toolName} for {consoleName} not found")
        );
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

    public static string GetNoIntroPath()
    {
        return GetGlobalPath("NoIntroDB");
    }

    public static string GetCachePath()
    {
        return GetGlobalPath("Cache");
    }
    #endregion

    #region Console-Specific Configuration
    public static string GetTitleDBPath(string consoleName)
    {
        return Path.Combine(
            BaseFolder,
            Configuration.GetSection($"Consoles:{consoleName}:TitleDB:Path")?.Value
                ?? throw new InvalidOperationException($"TitleDB path for {consoleName} not found")
        );
    }

    public static string GetTitleDBVersionsFile(string consoleName)
    {
        return Path.Combine(
            GetTitleDBPath(consoleName),
            Configuration.GetSection($"Consoles:{consoleName}:TitleDB:VersionsFile")?.Value
                ?? throw new InvalidOperationException($"Versions file for {consoleName} not found")
        );
    }
    #endregion
}
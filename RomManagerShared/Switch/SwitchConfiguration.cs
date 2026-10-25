using RomManagerShared.Configuration;
using System.IO;

namespace RomManagerShared.Switch.Configuration;

public static class SwitchConfiguration
{
    // ── Runtime overrides (set by the UI layer from user settings) ─────────
    /// <summary>Set by UI: overrides the TitleDB local path from config.json.</summary>
    public static string? TitleDBPathOverride { get; set; }
    /// <summary>Set by UI: overrides the TitleDB base URL from config.json.</summary>
    public static string? TitleDBUrlOverride { get; set; }
    /// <summary>Set by UI: overrides the versions URL from config.json.</summary>
    public static string? VersionsUrlOverride { get; set; }

    /// <summary>Local folder where Switch titledb JSON files are stored.</summary>
    public static string GetTitleDBPath()
    {
        // User override wins
        if (!string.IsNullOrWhiteSpace(TitleDBPathOverride))
            return Path.Combine(RomManagerConfiguration.BaseFolder, TitleDBPathOverride);

        if (!RomManagerConfiguration.ConfigurationLoaded) return string.Empty;
        var rel = RomManagerConfiguration.Configuration.GetSection("Switch:TitleDB:Path").Value;
        if (string.IsNullOrEmpty(rel)) return string.Empty;
        return Path.Combine(RomManagerConfiguration.BaseFolder, rel);
    }

    /// <summary>Base URL for per-region titledb JSON downloads (ends with /).</summary>
    public static string GetTitleDBUrl()
    {
        if (!string.IsNullOrWhiteSpace(TitleDBUrlOverride))
            return TitleDBUrlOverride;

        if (!RomManagerConfiguration.ConfigurationLoaded) return string.Empty;
        return RomManagerConfiguration.Configuration.GetSection("Switch:TitleDB:TitleDBUrl").Value
               ?? "https://raw.githubusercontent.com/blawar/titledb/master/";
    }

    public static string GetVersionsUrl()
    {
        if (!string.IsNullOrWhiteSpace(VersionsUrlOverride))
            return VersionsUrlOverride;

        if (!RomManagerConfiguration.ConfigurationLoaded) return string.Empty;
        return RomManagerConfiguration.Configuration.GetSection("Switch:TitleDB:VersionsUrl").Value
               ?? "https://raw.githubusercontent.com/blawar/titledb/master/versions.json";
    }

    public static string GetVersionsPath()
    {
        var dir = GetTitleDBPath();
        if (string.IsNullOrEmpty(dir)) return string.Empty;
        var file = RomManagerConfiguration.Configuration.GetSection("Switch:TitleDB:VersionsFile").Value ?? "versions.json";
        return Path.Combine(dir, file);
    }

    public static string[]? GetTitleDBRegionFiles()
    {
        if (!RomManagerConfiguration.ConfigurationLoaded) return null;
        var regionFilesSection = RomManagerConfiguration.Configuration.GetSection("Switch:TitleDB:RegionFiles");
        var regionFiles = regionFilesSection.GetChildren().Select(section => section.Value).Where(v => v != null).ToArray()!;
        return regionFiles.Length > 0 ? regionFiles : null;
    }

    public static string GetGlobalTitleDBUrl()
    {
        if (!RomManagerConfiguration.ConfigurationLoaded) return string.Empty;
        return RomManagerConfiguration.Configuration.GetSection("Switch:TitleDB:GlobalTitleDBUrl").Value
               ?? "https://raw.githubusercontent.com/blawar/titledb/master/US.en.json";
    }

    /// <summary>Returns just the filename (e.g. "US.en.json"), not the full path.</summary>
    public static string GetGlobalTitleDBPath()
    {
        if (!RomManagerConfiguration.ConfigurationLoaded) return "US.en.json";
        return RomManagerConfiguration.Configuration.GetSection("Switch:TitleDB:GlobalTitleDBFile").Value ?? "US.en.json";
    }
}



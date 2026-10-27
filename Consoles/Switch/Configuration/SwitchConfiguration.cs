using RomManagerShared.Configuration;

namespace RomManagerShared.Switch.Configuration;

/// <summary><c>Consoles:Switch</c> in config.json.</summary>
public sealed class SwitchConfigSection
{
    public SwitchTitleDbSection TitleDB { get; set; } = new();
}

public sealed class SwitchTitleDbSection
{
    public string Path { get; set; } = "Data/Switch/titledb";
    public string TitleDBUrl { get; set; } = "https://raw.githubusercontent.com/blawar/titledb/master/";
    public string GlobalTitleDBUrl { get; set; } = "https://raw.githubusercontent.com/blawar/titledb/master/US.en.json";
    public string GlobalTitleDBFile { get; set; } = "US.en.json";
    public string VersionsUrl { get; set; } = "https://raw.githubusercontent.com/blawar/titledb/master/versions.json";
    public string VersionsFile { get; set; } = "versions.json";
    // Null (not a default list): the binder appends to a non-empty default instead of replacing it.
    public string[]? RegionFiles { get; set; }
}

public static class SwitchConfiguration
{
    private static SwitchTitleDbSection TitleDb => RomManagerConfiguration.ForConsole<SwitchConfigSection>("Switch").TitleDB;

    // Runtime overrides, set from the user's Settings (SwitchOptions); they win over config.json.
    public static string? TitleDBPathOverride { get; set; }
    public static string? TitleDBUrlOverride { get; set; }
    public static string? VersionsUrlOverride { get; set; }

    /// <summary>Local folder where Switch titledb JSON files are stored.</summary>
    public static string GetTitleDBPath() => RomManagerConfiguration.UnderBase(TitleDBPathOverride, TitleDb.Path);

    /// <summary>Base URL for per-region titledb JSON downloads (ends with /).</summary>
    public static string GetTitleDBUrl() => NonEmpty(TitleDBUrlOverride) ?? TitleDb.TitleDBUrl;

    public static string GetVersionsUrl() => NonEmpty(VersionsUrlOverride) ?? TitleDb.VersionsUrl;

    public static string GetVersionsPath() => Path.Combine(GetTitleDBPath(), TitleDb.VersionsFile);

    public static string[]? GetTitleDBRegionFiles() => TitleDb.RegionFiles is { Length: > 0 } files ? files : null;

    public static string GetGlobalTitleDBUrl() => TitleDb.GlobalTitleDBUrl;

    /// <summary>Returns just the filename (e.g. "US.en.json"), not the full path.</summary>
    public static string GetGlobalTitleDBPath() => TitleDb.GlobalTitleDBFile;

    private static string? NonEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}

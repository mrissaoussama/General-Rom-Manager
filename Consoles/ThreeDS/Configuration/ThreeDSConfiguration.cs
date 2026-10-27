using RomManagerShared.Configuration;

namespace RomManagerShared.ThreeDS.Configuration;

/// <summary><c>Consoles:ThreeDS</c> in config.json.</summary>
public sealed class ThreeDSConfigSection
{
    public ThreeDSTitleDbSection TitleDB { get; set; } = new();
}

public sealed class ThreeDSTitleDbSection
{
    public string SavePath { get; set; } = "Data/ThreeDS/";
    public string BaseUrl { get; set; } = "https://github.com/hax0kartik/3dsdb/raw/master/jsons/";
    // Null when unset (the binder would append to a default list); see GetTitleDBRegionFilenames.
    public string[]? RegionFiles { get; set; }
}

public static class ThreeDSConfiguration
{
    private static readonly string[] DefaultRegionFiles = ["list_GB.json", "list_JP.json", "list_KR.json", "list_TW.json", "list_US.json"];

    private static ThreeDSTitleDbSection TitleDb => RomManagerConfiguration.ForConsole<ThreeDSConfigSection>("ThreeDS").TitleDB;

    public static string[] GetTitleDBRegionFilenames() => TitleDb.RegionFiles is { Length: > 0 } files ? files : DefaultRegionFiles;

    /// <summary>Folder of the region files, ending with a separator (callers append file names).</summary>
    public static string GetTitleDBPath() =>
        Path.TrimEndingDirectorySeparator(RomManagerConfiguration.UnderBase(TitleDb.SavePath)) + Path.DirectorySeparatorChar;

    public static string GetTitleDBBaseUrl() => TitleDb.BaseUrl;
}

using RomManagerShared.Configuration;

namespace RomManagerShared.Xbox360.Configuration;

/// <summary><c>Consoles:Xbox360</c> in config.json.</summary>
public sealed class Xbox360ConfigSection
{
    public string XexToolPath { get; set; } = "Tools/xextool.exe";
    public string ThumbnailCachePath { get; set; } = "Cache/Xbox360";
}

public static class Xbox360Configuration
{
    private static Xbox360ConfigSection Section => RomManagerConfiguration.ForConsole<Xbox360ConfigSection>("Xbox360");

    public static string GetXexToolPath() => RomManagerConfiguration.UnderBase(Section.XexToolPath);

    public static string GetThumbnailCachePath() => RomManagerConfiguration.UnderBase(Section.ThumbnailCachePath);
}

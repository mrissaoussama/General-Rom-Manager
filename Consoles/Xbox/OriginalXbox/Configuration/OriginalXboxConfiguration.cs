using RomManagerShared.Configuration;

namespace RomManagerShared.OriginalXbox.Configuration;

/// <summary><c>Consoles:OriginalXbox</c> in config.json.</summary>
public sealed class OriginalXboxConfigSection
{
    public string ThumbnailCachePath { get; set; } = "Cache/OriginalXbox";
}

public static class OriginalXboxConfiguration
{
    public static string GetThumbnailCachePath() =>
        RomManagerConfiguration.UnderBase(RomManagerConfiguration.ForConsole<OriginalXboxConfigSection>("OriginalXbox").ThumbnailCachePath);
}

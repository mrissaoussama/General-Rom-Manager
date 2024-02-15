using RomManagerShared.Configuration;

namespace RomManagerShared.OriginalXbox.Configuration;

public static class OriginalXboxConfiguration
{
    public static string GetThumbnailCachePath()
    {
        return RomManagerConfiguration.BaseFolder + RomManagerConfiguration.Configuration.GetSection("OriginalXbox:ThumbnailCachePath").Value!;
    }
}

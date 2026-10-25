using RomManagerShared.Configuration;

namespace RomManagerShared.Xbox360.Configuration;

public static class Xbox360Configuration
{
    public static string GetXexToolPath()
    {
        return RomManagerConfiguration.GetPathOrDefault("Xbox360:XexToolPath", "Tools/xextool.exe");
    }

    public static string GetThumbnailCachePath()
    {
        return RomManagerConfiguration.GetPathOrDefault("Xbox360:ThumbnailCachePath", "Cache/Xbox360");
    }
}

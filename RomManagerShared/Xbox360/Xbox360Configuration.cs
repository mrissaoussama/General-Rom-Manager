using RomManagerShared.Configuration;

namespace RomManagerShared.Xbox360.Configuration;

public static class Xbox360Configuration
{
    public static string GetXexToolPath()
    {
        return RomManagerConfiguration.BaseFolder + RomManagerConfiguration.Configuration.GetSection("Xbox360:XexToolPath").Value!;
    }

    public static string GetThumbnailCachePath()
    {
        return RomManagerConfiguration.BaseFolder + RomManagerConfiguration.Configuration.GetSection("Xbox360:ThumbnailCachePath").Value!;
    }
}

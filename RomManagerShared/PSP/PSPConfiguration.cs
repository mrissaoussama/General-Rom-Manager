using RomManagerShared.Configuration;

namespace RomManagerShared.PSP.Configuration;

public static class PSPConfiguration
{
    public static string GetCachePath()
    {
        return RomManagerConfiguration.BaseFolder + RomManagerConfiguration.Configuration.GetSection("PSP:CachePath").Value!;
    }
}

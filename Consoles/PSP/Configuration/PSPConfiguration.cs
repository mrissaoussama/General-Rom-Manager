using RomManagerShared.Configuration;

namespace RomManagerShared.PSP.Configuration;

public static class PSPConfiguration
{
    public static string GetCachePath()
    {
        return RomManagerConfiguration.GetPathOrDefault("PSP:CachePath", "Cache/PSP");
    }
}

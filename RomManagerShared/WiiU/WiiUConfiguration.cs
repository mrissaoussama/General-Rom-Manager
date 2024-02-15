using RomManagerShared.Configuration;

namespace RomManagerShared.WiiU.Configuration;

public static class WiiUConfiguration
{
    public static string GetTitleDBPath()
    {
        return RomManagerConfiguration.BaseFolder + RomManagerConfiguration.Configuration.GetSection("WiiU:TitleDB:TitleDBPath").Value!;
    }

    public static string GetTitleDBUrl()
    {
        return RomManagerConfiguration.BaseFolder + RomManagerConfiguration.Configuration.GetSection("WiiU:TitleDB:TitleDBUrl").Value!;
    }
}


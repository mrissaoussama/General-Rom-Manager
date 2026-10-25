using RomManagerShared.Configuration;

namespace RomManagerShared.WiiU.Configuration;

public static class WiiUConfiguration
{
    public static string GetTitleDBPath()
    {
        return RomManagerConfiguration.GetPathOrDefault("WiiU:TitleDB:Path", "Data/WiiU/titledb.xml");
    }

    public static string GetTitleDBUrl()
    {
        return  RomManagerConfiguration.Configuration.GetSection("WiiU:TitleDB:TitleDBUrl").Value!;
    }
}


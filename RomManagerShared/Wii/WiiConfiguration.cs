using RomManagerShared.Configuration;

namespace RomManagerShared.Wii.Configuration;

public static class WiiConfiguration
{
    public static string GetGameTDBPath()
    {
        return RomManagerConfiguration.BaseFolder + RomManagerConfiguration.Configuration.GetSection("Wii:GameTDB:Path").Value!;
    }

    public static string GetGameTDBUrl()
    {
        return RomManagerConfiguration.Configuration.GetSection("Wii:GameTDB:Url").Value!;
    }

    public static string GetWadCommonKeyPath()
    {
        return RomManagerConfiguration.BaseFolder + RomManagerConfiguration.Configuration.GetSection("Wii:Wad:CommonKeyPath").Value!;
    }
}


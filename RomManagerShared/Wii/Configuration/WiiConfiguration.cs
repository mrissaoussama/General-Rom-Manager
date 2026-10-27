using RomManagerShared.Configuration;

namespace RomManagerShared.Wii.Configuration;

public static class WiiConfiguration
{
    public static string GetGameTDBPath()
    {
        return RomManagerConfiguration.GetPathOrDefault("Wii:GameTDB:Path", "Data/Wii/wiitdb.xml");
    }

    public static string GetGameTDBUrl()
    {
        return RomManagerConfiguration.Configuration.GetSection("Wii:GameTDB:Url").Value!;
    }

    public static string GetWadCommonKeyPath()
    {
        return RomManagerConfiguration.GetPathOrDefault("Wii:Wad:CommonKeyPath", "Data/Wii/common-key.bin");
    }
}


using RomManagerShared.Configuration;

namespace RomManagerShared.WiiU.Configuration;

public static class WiiUConfiguration
{
    public static string GetTitleDBPath()
    {
        return RomManagerConfiguration.GetPathOrDefault("WiiU:TitleDB:Path", "Data/WiiU/titledb.xml");
    }

    /// <summary>Opt-in: cdecrypt extracts into the ROM folder.</summary>
    public static bool AllowCdecrypt()
    {
        return bool.TryParse(RomManagerConfiguration.Configuration?.GetSection("WiiU:AllowCdecrypt").Value, out var v) && v;
    }

    public static string GetTitleDBUrl()
    {
        return  RomManagerConfiguration.Configuration.GetSection("WiiU:TitleDB:TitleDBUrl").Value!;
    }
}


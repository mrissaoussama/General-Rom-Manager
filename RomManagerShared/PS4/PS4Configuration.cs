using RomManagerShared.Configuration;

namespace RomManagerShared.PS4.Configuration;


public static class PS4Configuration
{
    public static string GetToolsPath()
    {
        return RomManagerConfiguration.BaseFolder + RomManagerConfiguration.Configuration.GetSection("PS4:PS4ToolsPath").Value!;
    }
}



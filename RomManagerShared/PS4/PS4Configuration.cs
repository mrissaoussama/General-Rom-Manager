using RomManagerShared.Configuration;

namespace RomManagerShared.PS4.Configuration;


public static class PS4Configuration
{
    public static string GetToolsPath()
    {
        return RomManagerConfiguration.GetPathOrDefault("PS4:PS4ToolsPath", "Tools/PS4");
    }
}



using RomManagerShared.Configuration;

namespace RomManagerShared.Switch.Configuration;

public static class SwitchConfiguration
{
    public static string GetTitleDBPath()
    {
        return RomManagerConfiguration.BaseFolder +
            RomManagerConfiguration.Configuration.GetSection("Switch:TitleDB:TitleDBSavePath").Value!;
    }

    public static string GetTitleDBUrl()
    {
        return RomManagerConfiguration.Configuration.GetSection("Switch:TitleDB:TitleDBUrl").Value!;
    }

    public static string GetVersionsUrl()
    {
        return RomManagerConfiguration.Configuration.GetSection("Switch:TitleDB:VersionsUrl").Value!;
    }

    public static string GetVersionsPath()
    {
        return RomManagerConfiguration.BaseFolder + 
            RomManagerConfiguration.Configuration.GetSection("Switch:TitleDB:VersionsSavePath").Value!;
    }

    public static string[]? GetTitleDBRegionFiles()
    {
        var regionFilesSection = RomManagerConfiguration.Configuration.GetSection("Switch:TitleDB:RegionFiles");
        var regionFiles = regionFilesSection.GetChildren().Select(section => section.Value).ToArray();
        return regionFiles!;
    }

    public static string GetGlobalTitleDBUrl()
    {
        return RomManagerConfiguration.Configuration.GetSection("Switch:TitleDB:GlobalTitleDBUrl").Value!;
    }

    public static string GetGlobalTitleDBPath()
    {
        return RomManagerConfiguration.BaseFolder + RomManagerConfiguration.Configuration.GetSection("Switch:TitleDB:GlobalTitleDBPath").Value!;
    }
}



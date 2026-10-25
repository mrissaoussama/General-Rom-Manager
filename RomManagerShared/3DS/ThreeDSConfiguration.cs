using RomManagerShared.Configuration;

namespace RomManagerShared.ThreeDS.Configuration;


#region 3DS Configuration

public static class ThreeDSConfiguration
{
    public static string[]? GetTitleDBRegionFilenames()
    {
        var regionFilesSection = RomManagerConfiguration.Configuration.GetSection("ThreeDS:TitleDB:RegionFiles");
        var regionFiles = regionFilesSection.GetChildren().Select(section => section.Value).ToArray();
        return regionFiles!;
    }

    public static string GetTitleDBPath()
    {
        return RomManagerConfiguration.GetPathOrDefault("ThreeDS:TitleDB:SavePath", "Data/ThreeDS");
    }

    public static string GetTitleDBBaseUrl()
    {
        return RomManagerConfiguration.Configuration.GetSection("ThreeDS:TitleDB:BaseUrl").Value!;
    }
}

#endregion


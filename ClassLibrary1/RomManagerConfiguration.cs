using Microsoft.Extensions.Configuration;
namespace RomManagerShared;

public static class RomManagerConfiguration
{
    public static IConfigurationRoot Configuration { get; set; }    static public bool ConfigurationLoaded { get; set; }
    static string BaseFolder { get; set; }
    public static void Load(string jsonConfigPath)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(jsonConfigPath, optional: false);
        Configuration = builder.Build();
        BaseFolder = GetBaseFolderPath();
        if (!string.IsNullOrEmpty(BaseFolder))
            Directory.CreateDirectory(BaseFolder);
        else BaseFolder = string.Empty;
    }

    public static string GetBaseFolderPath()
    {
        string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appDataFolder, "GeneralRomManager\\");
    }
    public static string GetSqliteDBPath()
    {

        return Path.Combine(BaseFolder, Configuration.GetSection("SqliteDBPath").Value);
    }

    public static string GetSwitchTitleDBPath()
    {
        return BaseFolder + Configuration.GetSection("Switch:TitleDB:TitleDBSavePath").Value!;
    }
    public static string GetSwitchTitleDBUrl()
    {
        return Configuration.GetSection("Switch:TitleDB:TitleDBUrl").Value!;
    }
    public static string GetSwitchVersionsUrl()
    {
        return Configuration.GetSection("Switch:TitleDB:VersionsUrl").Value!;
    }
    public static string GetSwitchVersionsPath()
    {
        return BaseFolder + Configuration.GetSection("Switch:TitleDB:VersionsSavePath").Value!;
    }    public static string GetThreeDSTitleDBPath()
    {
        return BaseFolder + Configuration.GetSection("ThreeDS:TitleDB:SavePath").Value!;
    }
    public static string GetThreeDSTitleDBBaseUrl()
    {
        return Configuration.GetSection("ThreeDS:TitleDB:BaseUrl").Value!;
    }
    public static string[]? GetThreeDSTitleDBRegionFiles()
    {        var regionFilesSection = Configuration.GetSection("ThreeDS:TitleDB:RegionFiles");        var regionFiles = regionFilesSection.GetChildren().Select(section => section.Value).ToArray();        return regionFiles!;
    }
    public static string[]? GetSwitchTitleDBRegionFiles()
    {        var regionFilesSection = Configuration.GetSection("Switch:TitleDB:RegionFiles");        var regionFiles = regionFilesSection.GetChildren().Select(section => section.Value).ToArray();        return regionFiles!;
    }    public static string GetErrorLogPath()
    {
        var path = BaseFolder + Configuration.GetSection("ErrorLogPath").Value!;
        return path is null ? "ErrorLog.txt" : path;
    }    public static string GetPluginsPath()
    {
        var path = BaseFolder + Configuration.GetSection("PluginsPath").Value!;
        return path is null ? "Plugins" : path;
    }
    public static string GetWiiGameTDBPath()
    {
        return BaseFolder + Configuration.GetSection("Wii:GameTDB:Path").Value!;
    }    public static string GetWiiGameTDBUrl()
    {
        return Configuration.GetSection("Wii:GameTDB:Url").Value!;
    }    public static string GetWiiWadCommonKeyPath()
    {
        return BaseFolder + Configuration.GetSection("Wii:Wad:CommonKeyPath").Value!;
    }
    public static string GetPS4ToolsPath()
    {
        return BaseFolder + Configuration.GetSection("PS4:PS4ToolsPath").Value!;
    }

    public static string GetSwitchGlobalTitleDBUrl()
    {
        return Configuration.GetSection("Switch:TitleDB:GlobalTitleDBUrl").Value!;
    }

    public static string GetSwitchGlobalTitleDBPath()
    {
        return BaseFolder + Configuration.GetSection("Switch:TitleDB:GlobalTitleDBPath").Value!;
    }

    public static string GetXexToolPath()
    {
        return BaseFolder + Configuration.GetSection("Xbox360:XexToolPath").Value!;
    }

    public static string GetXbox360ThumbnailCachePath()
    {
        return BaseFolder + Configuration.GetSection("Xbox360:ThumbnailCachePath").Value!;
    }

    public static string GetOriginalXboxThumbnailCachePath()
    {
        return BaseFolder + Configuration.GetSection("OriginalXbox:ThumbnailCachePath").Value!;
    }

    internal static string GetWiiUTitleDBPath()
    {
        return BaseFolder + Configuration.GetSection("WiiU:TitleDB:TitleDBPath").Value!;
    }

    internal static string GetWiiUTitleDBUrl()
    {
        return BaseFolder + Configuration.GetSection("WiiU:TitleDB:TitleDBUrl").Value!;
    }

    internal static string GetPSPCachePath()
    {
        return BaseFolder + Configuration.GetSection("PSP:CachePath").Value!;
    }
}

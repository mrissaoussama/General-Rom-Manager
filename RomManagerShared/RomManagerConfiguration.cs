using Microsoft.Extensions.Configuration;
namespace RomManagerShared.Configuration;

public static class RomManagerConfiguration
{
    public static IConfigurationRoot Configuration { get; set; }
    static public bool ConfigurationLoaded { get; set; }
  public  static string BaseFolder { get; set; }

    public static void Load(string jsonConfigPath)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(jsonConfigPath, optional: false);
        Configuration = builder.Build();
        BaseFolder = GetBaseFolderPath();
        if (!string.IsNullOrEmpty(BaseFolder))
            Directory.CreateDirectory(BaseFolder);
        else
            BaseFolder = string.Empty;
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

    public static string GetErrorLogPath()
    {
        var path = BaseFolder + Configuration.GetSection("ErrorLogPath").Value!;
        return path is null ? "ErrorLog.txt" : path;
    }

    public static string GetPluginsPath()
    {
        var path = BaseFolder + Configuration.GetSection("PluginsPath").Value!;
        return path is null ? "Plugins" : path;
    }

    public static string GetNoIntroPath()
    {
        var path = BaseFolder + Configuration.GetSection("NoIntroPath").Value!;
        return path is null ? "NoIntro" : path;
    }
}


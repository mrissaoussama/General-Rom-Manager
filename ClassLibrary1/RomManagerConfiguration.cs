using Microsoft.Extensions.Configuration;
using System;
using Microsoft.Extensions.Configuration.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RomManagerShared
{
    public static class RomManagerConfiguration
    {
        public static IConfigurationRoot Configuration { get; set; }

        static public bool ConfigurationLoaded { get; set; }
        public static void Load(string jsonConfigPath)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(jsonConfigPath, optional: false);
            Configuration = builder.Build();
        }

        public static string? GetSwitchTitleDBPath()
        {
            return Configuration.GetSection("Switch:TitleDB:TitledbSavePath").Value;
        }
        public static string? GetSwitchTitleDBUrl()
        {
            return Configuration.GetSection("Switch:TitleDB:TitledbUrl").Value;
        }
        public static string? GetSwitchVersionsUrl()
        {
            return Configuration.GetSection("Switch:TitleDB:VersionsUrl").Value;
        }
        public static string? GetSwitchVersionsPath()
        {
            return Configuration.GetSection("Switch:TitleDB:VersionsSavePath").Value;
        }

        public static string? GetThreeDSTitleDBPath()
        {
            return Configuration.GetSection("ThreeDS:TitleDB:SavePath").Value;
        }
        public static string? GetThreeDSTitleDBBaseUrl()
        {
            return Configuration.GetSection("ThreeDS:TitleDB:BaseUrl").Value;
        }
        public static string[]? GetThreeDSTitleDBRegionFiles()
        {

            var regionFilesSection = Configuration.GetSection("ThreeDS:TitleDB:RegionFiles");

            var regionFiles = regionFilesSection.GetChildren().Select(section => section.Value).ToArray();

            return regionFiles!;
        }

        public static string GetErrorLogPath()
        {
            var path= Configuration.GetSection("ErrorLogPath").Value;
            if (path is null)
                return "ErrorLog.txt";
            return path;

        }

        public static string GetPluginsPath()
        {
            var path = Configuration.GetSection("PluginsPath").Value;
            if (path is null)
                return "Plugins";
            return path;
        }
        public static string? GetWiiGameTDBPath()
        {
            return Configuration.GetSection("Wii:GameTDB:Path").Value;
        }

        public static string? GetWiiGameTDBUrl()
        {
            return Configuration.GetSection("Wii:GameTDB:Url").Value;
        }

        public static string? GetWiiWadCommonKeyPath()
        {
            return Configuration.GetSection("Wii:Wad:CommonKeyPath").Value;
        }
        public static string? GetPS4ToolsPath()
        {
            return Configuration.GetSection("PS4:PS4ToolsPath").Value;
        }
    }
}

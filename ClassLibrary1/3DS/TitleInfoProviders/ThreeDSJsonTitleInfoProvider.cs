using RomManagerShared;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Intrinsics.Arm;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RomManagerShared.ThreeDS.TitleInfoProviders
{
    public class ThreeDSJsonTitleInfoProvider : ITitleInfoProvider
    {
        public string Source { get; set; }
        public Dictionary<string, JsonElement> TitlesDatabase { get; set; }
        private ThreeDSTitledbDownloader titledbDownloader;

        public ThreeDSJsonTitleInfoProvider(string jsonFilesDirectory)
        {
            titledbDownloader = new ThreeDSTitledbDownloader();
            Source = jsonFilesDirectory;
        }

        public async Task LoadTitleDatabaseAsync()
        {
            if (TitlesDatabase is not null)
                return;
            if (Source == null)
            {
                return;
            }
            var regionfiles = RomManagerConfiguration.GetThreeDSTitleDBRegionFiles();
            if (regionfiles is null || regionfiles.Length == 0)
            {
                FileUtils.Log("Region files unavailable");
                return;
            }
            if (!File.Exists(Source))
                await titledbDownloader.DownloadRegionFiles();
            TitlesDatabase = new Dictionary<string, JsonElement>();

            foreach (var regionFile in regionfiles)
            {
                string regionFilePath = Path.Combine(Source, regionFile);

                try
                {
                    var jsonContent = await File.ReadAllTextAsync(regionFilePath);
                    var regionTitles = JsonSerializer.Deserialize<List<JsonElement>>(jsonContent);

                    foreach (var title in regionTitles)
                    {
                        // Assuming "TitleID" is unique, you can use it as the key
                        var titleId = title.GetProperty("TitleID").GetString();
                        TitlesDatabase[titleId] = title;
                    }
                }
                catch (JsonException ex)
                {
                    // Log the error or handle it as needed
                    Console.WriteLine($"Error deserializing JSON from file '{regionFilePath}': {ex.Message}");
                }
            }
        }

        public async Task<IRom> GetTitleInfo(IRom rom)
        {
            if (TitlesDatabase.TryGetValue(rom.TitleID, out var titleInfoElement))
            {
                var titleInfoDto = new ThreeDSJsonDTO
                {
                    Name = titleInfoElement.GetProperty("Name").GetString(),
                    UID = titleInfoElement.GetProperty("UID").GetString(),
                    TitleID = titleInfoElement.GetProperty("TitleID").GetString(),
                    Version = NormalizeVersion(titleInfoElement.GetProperty("Version").GetString()),
                    ProductCode = titleInfoElement.GetProperty("Product Code").GetString(),
                    Publisher = titleInfoElement.GetProperty("Publisher").GetString(),
                    Size = ParseSize(titleInfoElement.GetProperty("Size").GetString())
                };

                rom.TitleName = titleInfoDto.Name;
                if(rom.Version==null ||rom.Version=="0"||int.Parse(rom.Version)<0)
                rom.Version = titleInfoDto.Version;
                rom.Publisher = titleInfoDto.Publisher;
                rom.ProductCode = titleInfoDto.ProductCode;
                rom.Size = titleInfoDto.Size;
            }
                if (rom.TitleID.Contains("0004008C"))
                {
                    string gameName = GetRelatedGameRomName(rom.TitleID);
                    if (rom is ThreeDSDLC)
                    {
                        if (!string.IsNullOrEmpty(gameName))
                            rom.TitleName = gameName + " DLC";
                        else
                            rom.TitleName = rom.TitleID + " " + "DLC";
                    }
                    else if (rom is ThreeDSUpdate)
                    {
                        if (!string.IsNullOrEmpty(gameName))
                            rom.TitleName = gameName + " Update";
                        else
                            rom.TitleName = rom.TitleID + " " + "Update";
                    }
                }
                return rom;
            
        }

        private string NormalizeVersion(string version)
        {
            return string.Equals(version, "N/A", StringComparison.OrdinalIgnoreCase) ? "0" : version;
        }

        private long ParseSize(string size)
        {
            if (string.Equals(size, "0B [N/A]", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            string sizeInBytes = size.Split(' ')[0];
            return long.TryParse(sizeInBytes, out var sizeValue) ? sizeValue : 0;
        }

        private string GetRelatedGameRomName(string titleID)
        {
            titleID=titleID.Replace("0004008C", "00040000");
            if (TitlesDatabase.TryGetValue(titleID, out var titleInfoElement))
            {
                var name = titleInfoElement.GetProperty("Name").GetString();
                return name ?? "";
            }
            return "";
        }

        public class ThreeDSJsonDTO
        {
            public string Name { get; set; }
            public string UID { get; set; }
            public string TitleID { get; set; }
            public string Version { get; set; }
            public long Size { get; set; }
            public string ProductCode { get; set; }
            public string Publisher { get; set; }
        }
    }
}

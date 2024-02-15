using RomManagerShared.Base;
using RomManagerShared.ThreeDS.Configuration;
using RomManagerShared.Utils;
using System.Text.Json;
namespace RomManagerShared.ThreeDS.TitleInfoProviders;

public class ThreeDSJsonTitleInfoProvider : ITitleInfoProvider
{
    public string Source { get; set; }
    public Dictionary<string, JsonElement> TitlesDatabase { get; set; }
    private readonly ThreeDSTitleDBDownloader titledbDownloader;    public ThreeDSJsonTitleInfoProvider(string jsonFilesDirectory)
    {
        titledbDownloader = new ThreeDSTitleDBDownloader();
        Source = jsonFilesDirectory;
    }    public async Task LoadTitleDatabaseAsync()
    {
        if (TitlesDatabase is not null)
            return;
        if (Source == null)
        {
            return;
        }
        var regionfiles = ThreeDSConfiguration.GetTitleDBRegionFilenames();
        if (regionfiles is null || regionfiles.Length == 0)
        {
            FileUtils.Log("3ds Region files unavailable");
            return;
        }
        if (!File.Exists(Source))
            await titledbDownloader.DownloadRegionFiles();
        TitlesDatabase = [];        foreach (var regionFile in regionfiles)
        {
            string regionFilePath = Path.Combine(Source, regionFile);            try
            {
                var jsonContent = await File.ReadAllTextAsync(regionFilePath);
                var regionTitles = JsonSerializer.Deserialize<List<JsonElement>>(jsonContent);                foreach (var title in regionTitles)
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
    }    public async Task<Rom> GetTitleInfo(Rom rom)
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
            };            rom.AddTitleName(titleInfoDto.Name);
            if (rom.Version == null || rom.Version == "0" || int.Parse(rom.Version) < 0)
                rom.Version = titleInfoDto.Version;
            rom.Publisher = titleInfoDto.Publisher;
            rom.ProductCode = titleInfoDto.ProductCode;
            rom.Size = titleInfoDto.Size;
        }

        return rom;    }    private static string NormalizeVersion(string version)
    {
        return string.Equals(version, "N/A", StringComparison.OrdinalIgnoreCase) ? "0" : version;
    }    private static long ParseSize(string size)
    {
        if (string.Equals(size, "0B [N/A]", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }        string sizeInBytes = size.Split(' ')[0];
        return long.TryParse(sizeInBytes, out var sizeValue) ? sizeValue : 0;
    }    private string GetRelatedGameRomName(string titleID)
    {
        titleID = titleID.Replace("0004008C", "00040000");
        if (TitlesDatabase.TryGetValue(titleID, out var titleInfoElement))
        {
            var name = titleInfoElement.GetProperty("Name").GetString();
            return name ?? "";
        }
        return "";
    }    public class ThreeDSJsonDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string UID { get; set; }
        public string TitleID { get; set; }
        public string Version { get; set; }
        public long Size { get; set; }
        public string ProductCode { get; set; }
        public string Publisher { get; set; }
    }
}

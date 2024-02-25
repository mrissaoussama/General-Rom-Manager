using Microsoft.EntityFrameworkCore.Metadata.Internal;
using RomManagerShared.Base;
using RomManagerShared.Base.Database;
using RomManagerShared.Base.Interfaces;
using RomManagerShared.Interfaces;
using RomManagerShared.ThreeDS.Configuration;
using RomManagerShared.Utils;
using RomManagerShared.WiiU;
using System.Text.Json;
namespace RomManagerShared.ThreeDS.TitleInfoProviders;

public class ThreeDSJsonTitleInfoProvider :TitleInfoProvider<ThreeDSConsole>,ICanSaveToDB
{
    public Dictionary<string, JsonElement>? TitlesJson { get; set; }
    public GenericRepository<ThreeDSJsonDTO> TitlesDatabaseRepository { get; set; }

    public ThreeDSJsonTitleInfoProvider(GenericRepository<ThreeDSJsonDTO> repo)
    {
        TitlesDatabaseRepository = repo;
        var regionspath = ThreeDSConfiguration.GetTitleDBPath();
        Source = regionspath;
    }    public override async Task LoadTitleDatabaseAsync()
    {
        if (TitlesJson is not null)
        {
            return;
        }

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
            await FileDownloader.DownloadThreeDSRegionFiles();
        TitlesJson = [];        foreach (var regionFile in regionfiles)
        {
            string regionFilePath = Path.Combine(Source, regionFile);            try
            {
                var jsonContent = await File.ReadAllTextAsync(regionFilePath);
                var regionTitles = JsonSerializer.Deserialize<List<JsonElement>>(jsonContent);                foreach (var title in regionTitles)
                {
                    // Assuming "TitleID" is unique, you can use it as the key
                    var titleId = title.GetProperty("TitleID").GetString();
                    TitlesJson[titleId] = title;
                }
            }
            catch (JsonException ex)
            {
                // Log the error or handle it as needed
                Console.WriteLine($"Error deserializing JSON from file '{regionFilePath}': {ex.Message}");
            }
        }
    }    public async Task SaveToDatabase()
    {
        if (TitlesJson is null||TitlesJson.Count == 0)
             return;
        List<ThreeDSJsonDTO> list = new();
        foreach (var title in TitlesJson)
        {
            list.Add(JsonElementToDto(title.Value));

        }
        await TitlesDatabaseRepository.AddOrUpdateByPropertyRangeAsync(list, typeof(ThreeDSJsonDTO), nameof(ThreeDSJsonDTO.TitleID));


    }    public override async Task<Rom> GetTitleInfo(Rom rom)
    {
        if (TitlesJson.TryGetValue(rom.TitleID, out var titleInfoElement))
        {
            ThreeDSJsonDTO titleInfoDto = JsonElementToDto(titleInfoElement);            rom.AddTitleName(titleInfoDto.Name);
            if (rom.Version == null || rom.Version == "0" || int.Parse(rom.Version) < 0)
                rom.Version = titleInfoDto.Version;
            rom.Publisher = titleInfoDto.Publisher;
            rom.ProductCode = titleInfoDto.ProductCode;
            rom.Size = titleInfoDto.Size;
        }

        return rom;    }

    private static ThreeDSJsonDTO JsonElementToDto(JsonElement titleInfoElement)
    {
        return new ThreeDSJsonDTO
        {
            Name = titleInfoElement.GetProperty("Name").GetString(),
            UID = titleInfoElement.GetProperty("UID").GetString(),
            TitleID = titleInfoElement.GetProperty("TitleID").GetString(),
            Version = ThreeDSJsonDTO.NormalizeVersion(titleInfoElement.GetProperty("Version").GetString()),
            ProductCode = titleInfoElement.GetProperty("Product Code").GetString(),
            Publisher = titleInfoElement.GetProperty("Publisher").GetString(),
            Size = ThreeDSJsonDTO.ParseSize(titleInfoElement.GetProperty("Size").GetString())
        };
    }

    public string GetRelatedGameRomName(string titleID)
    {
        titleID = titleID.Replace("0004008C", "00040000");
        if (TitlesJson.TryGetValue(titleID, out var titleInfoElement))
        {
            var name = titleInfoElement.GetProperty("Name").GetString();
            return name ?? "";
        }
        return "";
    }   
}
public class ThreeDSJsonDTO:IExternalRomFormat<ThreeDSConsole>
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? UID { get; set; }
    public string? TitleID { get; set; }
    public string? Version { get; set; }
    public long Size { get; set; }
    public string? ProductCode { get; set; }
    public string? Publisher { get; set; }
    public static string NormalizeVersion(string version)
    {
        return string.Equals(version, "N/A", StringComparison.OrdinalIgnoreCase) ? "0" : version;
    }
    public static long ParseSize(string size)
    {
        if (string.Equals(size, "0B [N/A]", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }
        string sizeInBytes = size.Split(' ')[0];
        return long.TryParse(sizeInBytes, out var sizeValue) ? sizeValue : 0;
    }

}
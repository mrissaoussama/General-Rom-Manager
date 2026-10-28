using Microsoft.EntityFrameworkCore.Metadata.Internal;
using RomManagerShared.Base;
using RomManagerShared.Base.Database;
using RomManagerShared.Base.Interfaces;
using RomManagerShared.Interfaces;
using RomManagerShared.ThreeDS.Configuration;
using RomManagerShared.Utils;
using System.Text.Json;
namespace RomManagerShared.ThreeDS.TitleInfoProviders;

public class ThreeDSJsonTitleInfoProvider  : TitleInfoProvider<ThreeDSConsole>
{
    public Dictionary<string, JsonElement>? TitlesJson { get; set; }

    public ThreeDSJsonTitleInfoProvider()
    {
        var regionspath = ThreeDSConfiguration.GetTitleDBPath();
        Source = regionspath;
    }
    public override async Task LoadTitleDatabaseAsync()
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
            AppLog.Warn("3ds Region files unavailable");
            return;
        }
        if (!File.Exists(Source))
            await ThreeDSDownloads.DownloadRegionFiles();
        TitlesJson = [];
        foreach (var regionFile in regionfiles)
        {
            string regionFilePath = Path.Combine(Source, regionFile);
            try
            {
                var jsonContent = await File.ReadAllTextAsync(regionFilePath);
                var regionTitles = JsonSerializer.Deserialize<List<JsonElement>>(jsonContent);
                foreach (var title in regionTitles ?? [])
                {
                    // Assuming "TitleID" is unique, you can use it as the key
                    var titleId = title.GetProperty("TitleID").GetString();
                    if (titleId is not null) TitlesJson[titleId] = title;
                }
            }
            catch (JsonException ex)
            {
                // Log the error or handle it as needed
                AppLog.Warn($"Error deserializing JSON from file '{regionFilePath}': {ex.Message}");
            }
        }
    }
    public override async Task<Rom> GetTitleInfo(Rom rom)
    {
        if (rom.TitleID is not null && TitlesJson is not null && TitlesJson.TryGetValue(rom.TitleID, out var titleInfoElement))
        {
            ThreeDSJsonDTO titleInfoDto = JsonElementToDto(titleInfoElement);
            if (titleInfoDto.Name is not null) rom.AddTitleName(titleInfoDto.Name);
            if (rom.Version == null || rom.Version == "0" || int.Parse(rom.Version) < 0)
                rom.Version = titleInfoDto.Version;
            // The file's own values win: the DB lists the eShop size, and blank fields must not clear what the header gave.
            if (string.IsNullOrWhiteSpace(rom.Publisher)) rom.Publisher = titleInfoDto.Publisher;
            if (string.IsNullOrWhiteSpace(rom.ProductCode)) rom.ProductCode = titleInfoDto.ProductCode;
            if (rom.Size is not > 0 && titleInfoDto.Size > 0) rom.Size = titleInfoDto.Size;
        }

        // Encrypted cartridge dumps have no readable icon: show GameTDB's box art (downloaded when first shown).
        if (rom is Game && string.IsNullOrWhiteSpace(rom.Icon) && string.IsNullOrWhiteSpace(rom.Thumbnail))
            rom.Thumbnail = ThreeDSUtils.GameTdbCoverUrl(rom.ProductCode);

        return rom;
    }

    private static ThreeDSJsonDTO JsonElementToDto(JsonElement titleInfoElement)
    {
        return new ThreeDSJsonDTO
        {
            Name = titleInfoElement.GetProperty("Name").GetString(),
            UID = titleInfoElement.GetProperty("UID").GetString(),
            TitleID = titleInfoElement.GetProperty("TitleID").GetString(),
            Version = ThreeDSJsonDTO.NormalizeVersion(titleInfoElement.GetProperty("Version").GetString() ?? ""),
            ProductCode = titleInfoElement.GetProperty("Product Code").GetString(),
            Publisher = titleInfoElement.GetProperty("Publisher").GetString(),
            Size = ThreeDSJsonDTO.ParseSize(titleInfoElement.GetProperty("Size").GetString() ?? "")
        };
    }

    public string GetRelatedGameRomName(string titleID)
    {
        titleID = titleID.Replace("0004008C", "00040000");
        if (TitlesJson is not null && TitlesJson.TryGetValue(titleID, out var titleInfoElement))
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
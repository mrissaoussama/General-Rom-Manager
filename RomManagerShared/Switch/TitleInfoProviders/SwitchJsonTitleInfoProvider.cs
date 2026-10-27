using RomManagerShared.Base;
using RomManagerShared.Base.Database;
using RomManagerShared.Base.Interfaces;
using RomManagerShared.Interfaces;
using RomManagerShared.Switch.Configuration;
using RomManagerShared.Utils;
using RomManagerShared.WiiU;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace RomManagerShared.Switch.TitleInfoProviders;

public class LongToStringConverter : JsonConverter<long?>
{
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt64(out long versionNumber))
            {
                return versionNumber;
            }
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            string? versionString = reader.GetString();
            if (versionString != null && long.TryParse(versionString, out long version))
            {
                return version;
            }
        }
        return null;

    }

    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
public class SwitchJsonRomDTO : IExternalRomFormat<SwitchConsole>
{
    public int Id { get; set; }
    [JsonPropertyName("name")]
    public string? TitleName { get; set; }
    [JsonPropertyName("id")]
    public string? TitleID { get; set; }
    [JsonPropertyName("version")]
    [JsonConverter(typeof(LongToStringConverter))]
    public long? Version { get; set; }
    [JsonPropertyName("region")]
    public string? Region { get; set; }
    [JsonPropertyName("iconUrl")]
    public string? Icon { get; set; }
    [JsonPropertyName("rating")]
    public int? Rating { get; set; }
    [JsonPropertyName("publisher")]
    public string? Publisher { get; set; }
    [JsonPropertyName("ratingContent")]
    public List<string>? RatingContent { get; set; }
    [JsonPropertyName("category")]
    public List<string>? Genres { get; set; }
    [JsonPropertyName("languages")]
    public List<string>? Languages { get; set; }
    [JsonPropertyName("developer")]
    public string? Developer { get; set; }
    [JsonPropertyName("size")]
    public long? Size { get; set; }
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    [JsonPropertyName("isDemo")]
    public bool? IsDemo { get; set; }
    [JsonPropertyName("numberOfPlayers")]
    public int? NumberOfPlayers { get; set; }
    [JsonPropertyName("releaseDate")]
    public int? ReleaseDate { get; set; }
    [JsonPropertyName("bannerUrl")]
    public string? Banner { get; set; }
    [JsonPropertyName("nsuId")]

    public long? NsuID { get; set; }
    [JsonPropertyName("screenshots")]
    public List<string>? Images { get; set; }
}
public class SwitchJsonTitleInfoProvider : TitleInfoProvider<SwitchConsole>, ICanSaveToDB
{
    public GenericRepository<SwitchJsonRomDTO> TitlesDatabaseRepository { get; set; }

    public Dictionary<string, JsonElement>? TitlesDatabase { get; set; }
    public SwitchJsonTitleInfoProvider(GenericRepository<SwitchJsonRomDTO> repo)
    {
        TitlesDatabaseRepository = repo;
    }
    public override async Task LoadTitleDatabaseAsync()
    {
        await FileDownloader.DownloadSwitchGlobalTitleDBFile();
        await FileDownloader.DownloadSwitchTitleDBFiles();

        Source = SwitchConfiguration.GetTitleDBPath();
        if (TitlesDatabase is not null)
            return;
        TitlesDatabase = [];
        if (string.IsNullOrEmpty(Source))
        {
            return; // no TitleDB folder configured
        }
        if (!Directory.Exists(Source))
        {
            try { Directory.CreateDirectory(Source); }
            catch (Exception ex)
            {
                FileUtils.Log($"[SwitchTitleDB] Could not create directory '{Source}': {ex.Message}");
                return;
            }
        }

        var globalJsonPath = Path.Combine(Source, SwitchConfiguration.GetGlobalTitleDBPath());
        if (File.Exists(globalJsonPath))
        {
            try
            {
                var globaljsonContent = await File.ReadAllTextAsync(globalJsonPath);
                var globaldb = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(globaljsonContent);
                if (globaldb is not null)
                    TitlesDatabase = TitlesDatabase.Union(globaldb)
                        .GroupBy(pair => pair.Key)
                        .ToDictionary(group => group.Key, group => group.First().Value);
            }
            catch (Exception ex) { FileUtils.Log($"[SwitchTitleDB] Error loading global titledb: {ex.Message}"); }
        }
        else
        {
            FileUtils.Log($"[SwitchTitleDB] Global titledb not found at '{globalJsonPath}'. Run 'Check for Updates' to download.");
        }

        foreach (var regionFile in SwitchConfiguration.GetTitleDBRegionFiles() ?? [])
        {
            if (string.IsNullOrEmpty(regionFile)) continue;
            var regionPath = Path.Combine(Source, regionFile);
            if (!File.Exists(regionPath)) { FileUtils.Log($"[SwitchTitleDB] Region file not found: {regionPath}"); continue; }
            try
            {
                var jsonContent = await File.ReadAllTextAsync(regionPath);
                var regiondb = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent);
                if (regiondb is not null)
                    TitlesDatabase = TitlesDatabase
                        .Union(regiondb)
                        .GroupBy(pair => pair.Key)
                        .ToDictionary(group => group.Key, group => group.First().Value);
            }
            catch (Exception ex) { FileUtils.Log($"[SwitchTitleDB] Error loading region file '{regionFile}': {ex.Message}"); }
        }


    }
    public async Task SaveToDatabase()
    {
        if (TitlesDatabase is null || TitlesDatabase.Count == 0)
            return;

        var duplicates = new List<SwitchJsonRomDTO>(); // List to store items with duplicate TitleID
        var frequencies = new Dictionary<string, int>(); // Dictionary to keep track of TitleID frequencies

        foreach (var title in TitlesDatabase)
        {
            var jsonString = title.Value.GetRawText();
            var romDto = JsonSerializer.Deserialize<SwitchJsonRomDTO>(jsonString);
            if (romDto is not null)
            {
                if (romDto.TitleID is not null)
                {
                    if (frequencies.ContainsKey(romDto.TitleID))
                    frequencies[romDto.TitleID]++;
                else
                    frequencies[romDto.TitleID] = 1;
              

                // Add to duplicates list if TitleID occurs more than once
                if (frequencies[romDto.TitleID] > 1)
                    duplicates.Add(romDto);  }
            }
        }

        // Filter list to include only items with duplicate TitleID values
        var list = TitlesDatabase.Values
            .Select(title => JsonSerializer.Deserialize<SwitchJsonRomDTO>(title.GetRawText()))
            .Where(romDto => romDto != null && romDto.TitleID!=null && frequencies.ContainsKey(romDto.TitleID) && frequencies[romDto.TitleID] > 1)
            .Select(romDto => romDto!)
            .ToList();

        // Call AddOrUpdateByPropertyRangeAsync with the filtered list
        await TitlesDatabaseRepository.AddOrUpdateByPropertyRangeAsync(list, typeof(SwitchJsonRomDTO), nameof(SwitchJsonRomDTO.TitleID));
    }

    public async override Task<Rom> GetTitleInfo(Rom rom)
    {
        // Not loaded yet (Setup skipped or download failed), or nothing to look up.
        if (TitlesDatabase is null || rom.TitleID is not { Length: > 0 } titleId)
            return rom;

        bool existsInDatabase = false;
        try
        {
            if (TitlesDatabase.TryGetValue(titleId, out var titleInfo))
            {
                existsInDatabase = true;
                var metadataClass = SwitchUtils.GetRomMetadataClass(titleId) ?? rom.GetType();
                UpdateRomIfDifferentType(ref rom, metadataClass);

                rom = MapToRom(rom, titleInfo, metadataClass);
                if (rom.Titles is not null)
                {
                    var valueswithid = rom.Titles.Where(x => x.Value.Contains(titleId)).ToList();
                    if (rom.Titles.Count == valueswithid.Count || rom.Titles.Count == 0)
                        if (IsUpdateOrDLC(metadataClass))
                        {
                            HandleUpdateOrDLCTitle(rom);
                        }
                }
                else
                {
                    if (IsUpdateOrDLC(metadataClass))
                    {
                        HandleUpdateOrDLCTitle(rom);
                    }
                }

                return rom;
            }
        }
        catch (Exception ex)
        {
            FileUtils.Log($"Error getting title info from JSON file: {ex.Message}");
        }

        FileUtils.Log($"Title ID '{rom.TitleID}' not found in db");

        if (!existsInDatabase)
        {
            var metadataClass = SwitchUtils.GetRomMetadataClass(titleId) ?? rom.GetType();
            UpdateRomIfDifferentType(ref rom, metadataClass);
            if (rom.Titles == null || rom.Titles.Count == 0 || rom.Titles.Count == rom.Titles.Where(x => x.Value.Contains(titleId)).ToList().Count)
                if (IsUpdateOrDLC(metadataClass))
                {
                    HandleUpdateOrDLCTitle(rom);
                }
        }

        return rom;
    }

    private static void UpdateRomIfDifferentType(ref Rom rom, Type metadataClass)
    {
        var romclasstype = rom.GetType();
        if (metadataClass != romclasstype)
        {
            var metadataInstance = (Rom)Activator.CreateInstance(metadataClass)!;
            RomUtils.CopyNonNullProperties(rom, metadataInstance);
            rom = metadataInstance;
        }
    }

    private static bool IsUpdateOrDLC(Type metadataClass)
    {
        return metadataClass == typeof(SwitchUpdate) || metadataClass == typeof(SwitchDLC);
    }

    private void HandleUpdateOrDLCTitle(Rom rom)
    {
        var relatedtitlename = GetRelatedGameName(rom);
        if (rom is SwitchDLC)
        {
            relatedtitlename += " DLC";
        }
        else if (rom is SwitchUpdate)
        {
            relatedtitlename += " Update";
        }

        if (rom.Version is not null and not "0")
        {
            relatedtitlename += " " + rom.Version;
        }
        if (rom.Titles != null)
        {
            if (rom.TitleID is { } id) rom.Titles.RemoveAll(x => x.Value.Contains(id));
            if (rom.Titles.Count == 0)
                rom.AddTitleName(relatedtitlename);
            else
            {
                rom.Titles.First().Value = relatedtitlename;
            }
        }

    }


    private string GetRelatedGameName(Rom rom)
    {
        if (rom.TitleID is null || TitlesDatabase is null) return string.Empty;
        var sharedID = SwitchUtils.GetIdentifyingTitleID(rom.TitleID);
        var gameId = sharedID + "000";

        for (int i = 0; i <= 15; i += 2)
        {
            char newHexDigit = i.ToString("X")[0];
            gameId = gameId[..12] + newHexDigit + gameId[13..];

            if (TitlesDatabase.TryGetValue(gameId, out var titleInfo))
            {
                var gamerom = MapToRom(rom, titleInfo, typeof(SwitchGame));
                _ = (rom is SwitchUpdate switchUpdate)
                  ? switchUpdate.RelatedGameTitleID = gamerom.TitleID
                    : (rom is SwitchDLC dlc) ? dlc.RelatedGameTitleID = gamerom.TitleID : null;

                if (gamerom.Titles is null || gamerom.Titles.Count == 0)
                    return string.Empty;
                if (gamerom.Titles.Count > 1)
                    gamerom.Titles.RemoveAll(x => x.Value.Contains(sharedID) || x.Value.Contains(sharedID));
                var gameName = $"{gamerom.Titles.First().Value}";
                return gameName;
            }
        }

        return string.Empty;
    }

    private static Rom MapToRom(Rom rom, JsonElement titleInfo, Type metadataClass)
    {
        // Copy non-null and non-default values from DTO to IRom
        if (JsonSerializer.Deserialize<SwitchJsonRomDTO>(titleInfo.GetRawText()) is { } romDto)
            CopyNonNullValues(romDto, rom);
        return rom;
    }
    private static void CopyNonNullValues(SwitchJsonRomDTO romDto, Rom rom)
    {
        // Set IsDemo to false if it's null
        rom.IsDemo = romDto.IsDemo ?? false;
        if (romDto.TitleName is not null && (rom.Titles is null || !rom.Titles.Any(x => x.Value.Contains(romDto.TitleName))))
        {
            rom.AddTitleName(romDto.TitleName);
            rom.Titles!.RemoveAll(x => (rom.TitleID is { } id && x.Value.Contains(id)) || (romDto.TitleID is { } dtoId && x.Value.Contains(dtoId)));

        }

        if (romDto.Description is not null)
            rom.AddDescription(romDto.Description);
        if (string.IsNullOrEmpty(rom.TitleID))
            rom.TitleID = romDto.TitleID ?? rom.TitleID;
        if (romDto.Version.HasValue && rom.Version is not null && (romDto.Version > long.Parse(rom.Version)))
        {
            rom.Version = romDto.Version?.ToString();
        }
        if (romDto.Region is not null)
            rom.AddRegion(romDto.Region);
        rom.Icon = romDto.Icon ?? rom.Icon; var rating = new Rating();

        if (romDto.RatingContent is not null)
        {


            if (romDto.RatingContent.Count != 0)
                rating.AddContent(romDto.RatingContent);
        }
        if (romDto.Rating is not null)

            rating.Age = (int)romDto.Rating;
        rom.AddRating(rating);
        rom.Publisher = romDto.Publisher ?? rom.Publisher;
        //rom.Genres = romDto.Genres ;
        //  rom.Languages = romDto.Languages ?? rom.Languages;
        rom.Developer = romDto.Developer ?? rom.Developer;
        rom.Size = romDto.Size ?? rom.Size;
        // rom.Description = romDto.Description ?? rom.Description;
        if (romDto.Languages is not null)
        {
            foreach (var language in romDto.Languages)
                rom.AddLanguage(LanguageUtils.ConvertToLanguage(language));
        }
        rom.NumberOfPlayers = romDto.NumberOfPlayers ?? rom.NumberOfPlayers;
        if (romDto.ReleaseDate != null)
        {
            int releasedate = (int)romDto.ReleaseDate;
            string datestring = releasedate.ToString();
            var dateonly = DateOnly.ParseExact(datestring
               ,
                "yyyyMMdd",
                CultureInfo.InvariantCulture);
            rom.ReleaseDate = dateonly;
        }
        rom.Banner = romDto.Banner ?? rom.Banner;
        rom.Images = romDto.Images ?? rom.Images;
    }
}

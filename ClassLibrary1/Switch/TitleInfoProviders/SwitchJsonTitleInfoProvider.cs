using RomManagerShared.Base;
using RomManagerShared.Utils;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace RomManagerShared.Switch.TitleInfoProviders
{
    public class VersionConverter : JsonConverter<long?>
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
                string versionString = reader.GetString();
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
    public class SwitchJsonRomDTO
    {
        [JsonPropertyName("name")]
        public string? TitleName { get; set; }        [JsonPropertyName("id")]
        public string? TitleID { get; set; }        [JsonPropertyName("version")]
        [JsonConverter(typeof(VersionConverter))]
        public long? Version { get; set; }        [JsonPropertyName("region")]
        public string? Region { get; set; }        [JsonPropertyName("iconUrl")]
        public string? Icon { get; set; }        [JsonPropertyName("rating")]
        public int? Rating { get; set; }        [JsonPropertyName("publisher")]
        public string? Publisher { get; set; }        [JsonPropertyName("ratingContent")]
        public List<string>? RatingContent { get; set; }        [JsonPropertyName("category")]
        public List<string>? Genres { get; set; }        [JsonPropertyName("languages")]
        public List<string>? Languages { get; set; }        [JsonPropertyName("developer")]
        public string? Developer { get; set; }        [JsonPropertyName("size")]
        public long? Size { get; set; }        [JsonPropertyName("description")]
        public string? Description { get; set; }        [JsonPropertyName("isDemo")]
        public bool? IsDemo { get; set; }        [JsonPropertyName("numberOfPlayers")]
        public int? NumberOfPlayers { get; set; }        [JsonPropertyName("releaseDate")]
        public int? ReleaseDate { get; set; }        [JsonPropertyName("bannerUrl")]
        public string? Banner { get; set; }        [JsonPropertyName("screenshots")]
        public HashSet<string>? Images { get; set; }
    }    public class SwitchJsonTitleInfoProvider : ITitleInfoProvider
    {
        public string Source { get; set; }
        public Dictionary<string, JsonElement> TitlesDatabase { get; set; }
        private readonly GithubDownloader titledbDownloader;        bool UseGlobalJson;#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public SwitchJsonTitleInfoProvider(string jsonFilePath, bool useGlobalJson = true)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            titledbDownloader = new GithubDownloader();
            Source = jsonFilePath;
            UseGlobalJson = useGlobalJson;
        }        public async Task LoadTitleDatabaseAsync()
        {
            if (TitlesDatabase is not null)
                return;
            TitlesDatabase = [];
            if (Source == null)
            {
                return;
            }
            if (!Directory.Exists(Source))
            {
                Directory.CreateDirectory(Source);
            }


            await titledbDownloader.DownloadSwitchGlobalTitleDBFile();
            var globaljsonContent = await File.ReadAllTextAsync(Path.Combine(Source, RomManagerConfiguration.GetSwitchGlobalTitleDBPath()));
            var globaldb = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(globaljsonContent);
            TitlesDatabase = TitlesDatabase.Union(globaldb)
     .GroupBy(pair => pair.Key)
     .ToDictionary(group => group.Key, group => group.First().Value);
            await titledbDownloader.DownloadSwitchTitleDBFiles();
            foreach (var regionFile in RomManagerConfiguration.GetSwitchTitleDBRegionFiles())
            {
                var jsonContent = await File.ReadAllTextAsync(Path.Combine(Source, regionFile));
                var regiondb = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent);
                TitlesDatabase = TitlesDatabase
     .Union(regiondb)
     .GroupBy(pair => pair.Key)
     .ToDictionary(group => group.Key, group => group.First().Value);
            }
            //}
            //else


        }
        public async Task<Rom> GetTitleInfo(Rom rom)
        {

            bool existsInDatabase = false;
            try
            {
                if (TitlesDatabase.TryGetValue(rom.TitleID, out var titleInfo))
                {
                    existsInDatabase = true;
                    var metadataClass = SwitchUtils.GetRomMetadataClass(rom.TitleID);
                    UpdateRomIfDifferentType(ref rom, metadataClass);

                    rom = MapToIRom(rom, titleInfo, metadataClass);
                    if (rom.Titles is not null)
                    {
                        var valueswithid = rom.Titles.Where(x => x.Value.Contains(rom.TitleID)).ToList();
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
                var metadataClass = SwitchUtils.GetRomMetadataClass(rom.TitleID);
                UpdateRomIfDifferentType(ref rom, metadataClass);
                if (rom.Titles == null || rom.Titles.Count == 0 || rom.Titles.Count == rom.Titles.Where(x => x.Value.Contains(rom.TitleID)).ToList().Count)
                    if (IsUpdateOrDLC(metadataClass))
                    {
                        HandleUpdateOrDLCTitle(rom);
                    }
            }

            return rom;
        }

        private void UpdateRomIfDifferentType(ref Rom rom, Type metadataClass)
        {
            var romclasstype = rom.GetType();
            if (metadataClass != romclasstype)
            {
                var metadataInstance = Activator.CreateInstance(metadataClass);
                RomUtils.CopyRomProperties(rom, (Rom)metadataInstance);
                rom = (Rom)metadataInstance;
            }
        }

        private bool IsUpdateOrDLC(Type metadataClass)
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

            if (rom.Version is not null && rom.Version != "0")
            {
                relatedtitlename += " " + rom.Version;
            }
            if (rom.Titles != null)
            {
                rom.Titles.RemoveWhere(x => x.Value.Contains(rom.TitleID));
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
            var sharedID = SwitchUtils.GetIdentifyingTitleID(rom.TitleID);
            var gameId = sharedID + "000";

            for (int i = 0; i <= 15; i += 2)
            {
                char newHexDigit = i.ToString("X")[0];
                gameId = gameId.Substring(0, 12) + newHexDigit + gameId.Substring(13);

                if (TitlesDatabase.TryGetValue(gameId, out var titleInfo))
                {
                    var gamerom = MapToIRom(rom, titleInfo, typeof(SwitchGame));
                    _ = (rom is SwitchUpdate switchUpdate)
                      ? switchUpdate.RelatedGameTitleID = gamerom.TitleID
                        : (rom is SwitchDLC dlc) ? dlc.RelatedGameTitleID = gamerom.TitleID : null;

                    if (gamerom.Titles is null || gamerom.Titles.Count == 0)
                        return string.Empty;
                    if (gamerom.Titles.Count > 1)
                        gamerom.Titles.RemoveWhere(x => x.Value.Contains(sharedID) || x.Value.Contains(sharedID));
                    var gameName = $"{gamerom.Titles.First().Value}";
                    return gameName;
                }
            }

            return string.Empty;
        }
        private static Rom MapToIRom(Rom rom, JsonElement titleInfo, Type metadataClass)
        {
            if (metadataClass != null)
            {
                var jsonString = titleInfo.GetRawText();
                var romDto = JsonSerializer.Deserialize<SwitchJsonRomDTO>(jsonString);                // Copy non-null and non-default values from DTO to IRom
                CopyNonNullValues(romDto, rom);                return rom;
            }
            else
            {
                return null;
            }
        }        private static void CopyNonNullValues(SwitchJsonRomDTO romDto, Rom rom)
        {
            // Set IsDemo to false if it's null
            rom.IsDemo = romDto.IsDemo ?? false;
            if (romDto.TitleName is not null && (rom.Titles is null || !rom.Titles.Any(x => x.Value.Contains(romDto.TitleName))))
            {
                rom.AddTitleName(romDto.TitleName);
                rom.Titles.RemoveWhere(x => x.Value.Contains(rom.TitleID) || x.Value.Contains(romDto.TitleID));

            }

            if (romDto.Description is not null)
                rom.AddDescription(romDto.Description);
            if (string.IsNullOrEmpty(rom.TitleID))
                rom.TitleID = romDto.TitleID ?? rom.TitleID;
            if (romDto.Version.HasValue && (rom.Version is not null && (romDto.Version > long.Parse(rom.Version))))
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
            rom.NumberOfPlayers = romDto.NumberOfPlayers ?? rom.NumberOfPlayers;            if (romDto.ReleaseDate != null)
            {
                int releasedate = (int)romDto.ReleaseDate;
                string datestring = releasedate.ToString();
                var dateonly = DateOnly.ParseExact(datestring
                   ,
                    "yyyyMMdd",
                    CultureInfo.InvariantCulture);
                rom.ReleaseDate = dateonly;
            }            rom.Banner = romDto.Banner ?? rom.Banner;
            rom.Images = romDto.Images ?? rom.Images;
        }
    }
}

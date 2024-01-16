using RomManagerShared.Base;
using RomManagerShared.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RomManagerShared.Switch.TitleInfoProviders
{
    public class SwitchJsonRomDTO
    {
        [JsonPropertyName("name")]
        public string? TitleName { get; set; }

        [JsonPropertyName("id")]
        public string? TitleID { get; set; }

        [JsonPropertyName("version")]
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

        [JsonPropertyName("screenshots")]
        public List<string>? Images { get; set; }
    }

    public class SwitchJsonTitleInfoProvider : ITitleInfoProvider
    {
        public string Source { get; set; }
        public Dictionary<string, JsonElement> TitlesDatabase { get; set; }
        private readonly SwitchTitledbDownloader titledbDownloader;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public SwitchJsonTitleInfoProvider(string jsonFilePath)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            titledbDownloader = new SwitchTitledbDownloader();
            Source = jsonFilePath;
        }

        public async Task LoadTitleDatabaseAsync()
        {
            if (TitlesDatabase is not null)
                return;
            if (Source == null)
            {
                return;
            }
            if (!File.Exists(Source))
                await titledbDownloader.DownloadTitledbFile();
            var jsonContent = await File.ReadAllTextAsync(Source);
#pragma warning disable CS8601 // Possible null reference assignment.
            TitlesDatabase = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent);
#pragma warning restore CS8601 // Possible null reference assignment.
        }

        public async Task<Rom> GetTitleInfo(Rom rom)
        {
            try
            {
                if (TitlesDatabase.TryGetValue(rom.TitleID, out var titleInfo))
                {
                    var metadataClass = SwitchUtils.GetRomMetadataClass(rom.TitleID);
                    if (metadataClass != rom.GetType())
                    {
                        // Create an instance of the metadataClass and apply the values from romDto
                        var metadataInstance = Activator.CreateInstance(metadataClass);
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                        RomUtils.CopyIRomProperties(rom, (Rom)metadataInstance);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                        rom = (Rom)metadataInstance;
                    }

                    rom = MapToIRom(rom, titleInfo, metadataClass);

                    if (metadataClass == typeof(SwitchUpdate))
                    {
                        rom.TitleName = GetUpdateName(rom, rom.Version);
                    }
                    return rom;
                }
                else
                {
                    FileUtils.Log($"Title ID '{rom.TitleID}' not found");
                    return rom;
                }
            }
            catch (Exception ex)
            {
                FileUtils.Log($"Error getting title info from JSON file: {ex.Message}");
                return rom;
            }
        }

    

        private string GetUpdateName(Rom rom, string version)
        {
            rom.TitleID = rom.TitleID.Remove(rom.TitleID.Length - 3, 3);
            rom.TitleID += "000";
            
            if (TitlesDatabase.TryGetValue(rom.TitleID, out var titleInfo))
            {
                var gamerom = MapToIRom(rom, titleInfo, typeof(SwitchGame));
                var updateName = $"{gamerom.TitleName}[{version}]";
                return updateName;
            }
            return "Update Name Not Found";
        }

 
        private static Rom MapToIRom(Rom rom, JsonElement titleInfo, Type metadataClass)
        {
            if (metadataClass != null)
            {
                var jsonString = titleInfo.GetRawText();
                var romDto = JsonSerializer.Deserialize<SwitchJsonRomDTO>(jsonString);

                // Copy non-null and non-default values from DTO to IRom
                CopyNonNullValues(romDto, rom);

                return rom;
            }
            else
            {
                return null;
            }
        }

        private static void CopyNonNullValues(SwitchJsonRomDTO romDto, Rom rom)
        {
            // Set IsDemo to false if it's null
            rom.IsDemo = romDto.IsDemo ?? false;

            // Copy non-null values from DTO to IRom
            rom.TitleName = romDto.TitleName ?? rom.TitleName;
            rom.TitleID = romDto.TitleID ?? rom.TitleID;
            if (romDto.Version.HasValue && (romDto.Version > long.Parse(rom.Version)))
            {
#pragma warning disable CS8601 // Possible null reference assignment.
                rom.Version = romDto.Version?.ToString();
#pragma warning restore CS8601 // Possible null reference assignment.
            }
            rom.Region = romDto.Region ?? rom.Region;
            rom.Icon = romDto.Icon ?? rom.Icon;
            rom.Rating = romDto.Rating.ToString() ?? rom.Rating;
            rom.Publisher = romDto.Publisher ?? rom.Publisher;
            rom.RatingContent = romDto.RatingContent ?? rom.RatingContent;
            rom.Genres = romDto.Genres ?? rom.Genres;
            rom.Languages = romDto.Languages ?? rom.Languages;
            rom.Developer = romDto.Developer ?? rom.Developer;
            rom.Size = romDto.Size ?? rom.Size;
            rom.Description = romDto.Description ?? rom.Description;
            rom.NumberOfPlayers = romDto.NumberOfPlayers ?? rom.NumberOfPlayers;

            if (romDto.ReleaseDate != null)
            { int releasedate = (int)romDto.ReleaseDate;
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
}

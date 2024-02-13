using RomManagerShared.Base;
using RomManagerShared.Utils;
using System.Globalization;
using System.Xml;namespace RomManagerShared.Wii.TitleInfoProviders;

public class WiiXmlRomDTO
{
    public string TitleID { get; set; }
    public string Region { get; set; }
    public string Languages { get; set; }
    public string Developer { get; set; }
    public string Publisher { get; set; }
    public int? ReleaseDate { get; set; }
    public int Players { get; set; } // Players attribute for input
    public string Version { get; set; } // Version attribute for rom
    public long? Size { get; set; } // Size attribute for rom
    public string? Title { get; internal set; }
    public string? Synopsis { get; internal set; }
}
public class WiiGameTDBInfoProvider : ITitleInfoProvider
{    public string Source { get; set; }
    public List<WiiXmlRomDTO> TitleList { get; private set; }    public async Task LoadTitleDatabaseAsync()
    {
        Source = RomManagerConfiguration.GetWiiGameTDBPath();
        try
        {
            if (!File.Exists(Source))
            {
                var gametdbdownloader = new GameTDBDownloader();
                var gameTDBPath = RomManagerConfiguration.GetWiiGameTDBPath();
                var gameTDBUrl = RomManagerConfiguration.GetWiiGameTDBUrl();                await GameTDBDownloader.DownloadAndExtractZip(gameTDBUrl, gameTDBPath);
            }
            TitleList = [];
            XmlDocument xmlDoc = new();
            xmlDoc.Load(Source);
            XmlNodeList gameNodes = xmlDoc.SelectNodes("//game");
            if (gameNodes != null)
            {
                foreach (XmlNode gameNode in gameNodes)
                {
                    var romDto = DeserializeGameElement(gameNode);
                    TitleList.Add(romDto);
                }
            }        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading Wii XML title database: {ex.Message}");
        }
    }    public async Task<Rom> GetTitleInfo(Rom rom)
    {
        try
        {
            if (TitleList != null)
            {
                foreach (var romDto in TitleList)
                {
                    var titleID = romDto.TitleID;                    if (titleID == rom.TitleID)
                    {
                        rom = MapToIRom(romDto, rom);
                        return rom;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting title info from Wii XML file: {ex.Message}");
        }        return rom;
    }    private static WiiXmlRomDTO DeserializeGameElement(XmlNode gameNode)
    {
        var romDto = new WiiXmlRomDTO();        foreach (XmlNode propertyNode in gameNode.ChildNodes)
        {
            switch (propertyNode.Name)
            {
                case "id":
                    romDto.TitleID = propertyNode.InnerText;
                    break;
                case "region":
                    romDto.Region = propertyNode.InnerText;
                    break;
                case "languages":
                    romDto.Languages = propertyNode.InnerText;
                    break;
                case "locale":
                    XmlNode enLocaleNode = gameNode.SelectSingleNode("locale[@lang='EN']");
                    //look for english title, if it doesn't exist use the first locale
                    enLocaleNode ??= gameNode.SelectSingleNode("locale");                    if (enLocaleNode != null)
                    {
                        romDto.Title = enLocaleNode.SelectSingleNode("title")?.InnerText;
                        romDto.Synopsis = enLocaleNode.SelectSingleNode("synopsis")?.InnerText;
                    }
                    break;                case "developer":
                    romDto.Developer = propertyNode.InnerText;
                    break;
                case "publisher":
                    romDto.Publisher = propertyNode.InnerText;
                    break;
                case "date":
                    var yearAttr = propertyNode.Attributes["year"];
                    var monthAttr = propertyNode.Attributes["month"];
                    var dayAttr = propertyNode.Attributes["day"];                    if (yearAttr != null && monthAttr != null && dayAttr != null &&
                        int.TryParse(yearAttr.Value, out var year) &&
                        int.TryParse(monthAttr.Value, out var month) &&
                        int.TryParse(dayAttr.Value, out var day))
                    {
                        if (string.IsNullOrEmpty(yearAttr.Value))
                            year = 0000;                        if (string.IsNullOrEmpty(monthAttr.Value))
                            month = 00;                        if (string.IsNullOrEmpty(dayAttr.Value))
                            day = 00;                        int releaseDate = (year * 10000) + (month * 100) + day;                        romDto.ReleaseDate = releaseDate;
                    }
                    break;                case "wi-fi":
                    break;
                case "input":
                    var playersAttribute = propertyNode.Attributes["players"];
                    if (playersAttribute != null)
                    {
                        romDto.Players = int.Parse(playersAttribute.Value);
                    }
                    break;
                case "rom":
                    var versionAttribute = propertyNode.Attributes["version"];
                    var sizeAttribute = propertyNode.Attributes["size"];
                    if (versionAttribute != null)
                    {
                        romDto.Version = versionAttribute.Value;
                    }
                    if (sizeAttribute != null)
                    {
                        romDto.Size = long.Parse(sizeAttribute.Value);
                    }
                    break;
            }
        }        return romDto;
    }    private static Rom MapToIRom(WiiXmlRomDTO romDto, Rom rom)
    {
        // Set properties from DTO to IRom
        rom.AddTitleName(romDto.Title);
        rom.TitleID = romDto.TitleID;
        rom.Version = romDto.Version?.ToString();
        rom.AddRegion(Enum.Parse<Region>(romDto.Region));        rom.Publisher = romDto.Publisher;
        rom.AddLanguage(Enum.Parse<Language>(romDto.Languages));
        rom.Developer = romDto.Developer;
        rom.Size = romDto.Size ?? FileUtils.GetFileSize(rom.Path); ;
        rom.AddDescription(romDto.Synopsis);
        rom.NumberOfPlayers = romDto.Players;        if (romDto.ReleaseDate != null)
        {
            int releasedate = (int)romDto.ReleaseDate;
            string datestring = releasedate.ToString();
            var dateonly = DateOnly.ParseExact(datestring, "yyyyMMdd", CultureInfo.InvariantCulture);
            rom.ReleaseDate = dateonly;
        }        return rom;
    }
}
﻿using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
using RomManagerShared.Wii.Configuration;
using System.Globalization;
using System.Xml;

public class WiiGameTDBXmlRomDTO
{
    public int Id { get; set; }
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
public class WiiGameTDBInfoProvider : ITitleInfoProvider<WiiConsole>
{
    public List<WiiGameTDBXmlRomDTO> TitleList { get; private set; }
    {
        Source = WiiConfiguration.GetGameTDBPath();
        try
        {
            if (!File.Exists(Source))
            {
                var gameTDBPath = WiiConfiguration.GetGameTDBPath();
                var gameTDBUrl = WiiConfiguration.GetGameTDBUrl();
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
            }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading Wii XML title database: {ex.Message}");
        }
    }
    {
        try
        {
            if (TitleList != null)
            {
                foreach (var romDto in TitleList)
                {
                    var titleID = romDto.TitleID;
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
        }
    }
    {
        var romDto = new WiiGameTDBXmlRomDTO();
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
                    enLocaleNode ??= gameNode.SelectSingleNode("locale");
                    {
                        romDto.Title = enLocaleNode.SelectSingleNode("title")?.InnerText;
                        romDto.Synopsis = enLocaleNode.SelectSingleNode("synopsis")?.InnerText;
                    }
                    break;
                    romDto.Developer = propertyNode.InnerText;
                    break;
                case "publisher":
                    romDto.Publisher = propertyNode.InnerText;
                    break;
                case "date":
                    var yearAttr = propertyNode.Attributes["year"];
                    var monthAttr = propertyNode.Attributes["month"];
                    var dayAttr = propertyNode.Attributes["day"];
                        int.TryParse(yearAttr.Value, out var year) &&
                        int.TryParse(monthAttr.Value, out var month) &&
                        int.TryParse(dayAttr.Value, out var day))
                    {
                        if (string.IsNullOrEmpty(yearAttr.Value))
                            year = 0000;
                            month = 00;
                            day = 00;
                    }
                    break;
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
        }
    }
    {
        // Set properties from DTO to IRom
        rom.AddTitleName(romDto.Title);
        rom.TitleID = romDto.TitleID;
        rom.Version = romDto.Version?.ToString();
        rom.AddRegion(Enum.Parse<Region>(romDto.Region));
        rom.AddLanguage(Enum.Parse<Language>(romDto.Languages));
        rom.Developer = romDto.Developer;
        rom.Size = romDto.Size ?? FileUtils.GetFileSize(rom.Path); ;
        rom.AddDescription(romDto.Synopsis);
        rom.NumberOfPlayers = romDto.Players;
        {
            int releasedate = (int)romDto.ReleaseDate;
            string datestring = releasedate.ToString();
            var dateonly = DateOnly.ParseExact(datestring, "yyyyMMdd", CultureInfo.InvariantCulture);
            rom.ReleaseDate = dateonly;
        }
    }
}
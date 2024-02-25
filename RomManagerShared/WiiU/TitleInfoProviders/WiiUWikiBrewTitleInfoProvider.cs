using RomManagerShared.Base;
using RomManagerShared.Base.Database;
using RomManagerShared.Interfaces;
using RomManagerShared.WiiU.Configuration;
using RomManagerShared.Utils;
using System.Text.Json;
using LibHac.Tools.Fs;
using RomManagerShared.Switch.TitleInfoProviders;
using System.Collections.Generic;
using RomManagerShared.Base.Interfaces;
namespace RomManagerShared.WiiU.TitleInfoProviders;

public class WiiUWikiBrewTitleInfoProvider : TitleInfoProvider<WiiUConsole>,ICanSaveToDB
{
    public GenericRepository<WiiUWikiBrewTitleDTO> TitlesDatabaseRepository { get; set; }
    public WiiUWikiBrewTitleInfoProvider(GenericRepository<WiiUWikiBrewTitleDTO> repo)
    {
        TitlesDatabaseRepository = repo;
    }
    public override  Task LoadTitleDatabaseAsync()
    {
       return WiiUWikiBrewScraper.ScrapeTitles();
    }
    public async Task SaveToDatabase()
    {
        if (WiiUWikiBrewScraper.titles is null || WiiUWikiBrewScraper.titles.Count == 0)
            return;
        await TitlesDatabaseRepository.AddOrUpdateByPropertyRangeAsync(WiiUWikiBrewScraper.titles, typeof(WiiUWikiBrewTitleDTO), nameof(WiiUWikiBrewTitleDTO.TitleID));

    }    public override async Task<Rom> GetTitleInfo(Rom rom)
    {
        WiiUWikiBrewTitleDTO? title = WiiUWikiBrewScraper.titles?.FirstOrDefault(x =>
                (rom.TitleID != null && x.TitleID == rom.TitleID) ||
                (x.ProductCode != null && rom.ProductCode != null && rom.ProductCode.Contains(x.ProductCode)));
        if (title is not null)
        {
           return WiiUWikiBrewTitleDTO.ToRom(rom, title);
        }return rom;
       
    }

   

}

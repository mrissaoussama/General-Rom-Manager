using RomManagerShared.Base;
using RomManagerShared.Base.Database;
using RomManagerShared.Interfaces;
using RomManagerShared.WiiU.Configuration;
using RomManagerShared.Utils;
using System.Text.Json;
namespace RomManagerShared.WiiU.TitleInfoProviders;

public class WiiUDatabaseTitleInfoProvider : TitleInfoProvider<WiiUConsole>
{
    public GenericRepository<WiiUWikiBrewTitleDTO> TitlesDatabase { get; set; }
    public WiiUDatabaseTitleInfoProvider(GenericRepository<WiiUWikiBrewTitleDTO> repo)
    {
        TitlesDatabase = repo;
    }    public override async Task<Rom> GetTitleInfo(Rom rom)
    {

        // Disc images only expose a product code.
        var titleInfoDto = rom.TitleID is not null
            ? await TitlesDatabase.GetByPropertyAsync(nameof(WiiUWikiBrewTitleDTO.TitleID), rom.TitleID)
            : rom.ProductCode is not null
                ? await TitlesDatabase.GetByPropertyAsync(nameof(WiiUWikiBrewTitleDTO.ProductCode), rom.ProductCode)
                : null;
        if (titleInfoDto is not null) { 
            
           var newrom= WiiUWikiBrewTitleDTO.ToRom(rom, titleInfoDto);
            return newrom;
        }
        else
        {
            FileUtils.Log("rom not found in db");
        }
        return rom;
    }

   

}

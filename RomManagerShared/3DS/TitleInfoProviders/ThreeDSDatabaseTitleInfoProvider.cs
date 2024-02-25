using RomManagerShared.Base;
using RomManagerShared.Base.Database;
using RomManagerShared.Interfaces;
using RomManagerShared.ThreeDS.Configuration;
using RomManagerShared.Utils;
using System.Text.Json;
namespace RomManagerShared.ThreeDS.TitleInfoProviders;

public class ThreeDSDatabaseTitleInfoProvider : TitleInfoProvider<ThreeDSConsole>
{
    public GenericRepository<ThreeDSJsonDTO> TitlesDatabase { get; set; }
    public ThreeDSDatabaseTitleInfoProvider(GenericRepository<ThreeDSJsonDTO> repo)
    {
        TitlesDatabase = repo;
    }    public override async Task<Rom> GetTitleInfo(Rom rom)
    {
        var titleInfoDto =await TitlesDatabase.GetByPropertyAsync(nameof(ThreeDSJsonDTO.TitleID), rom.TitleID);
        if (titleInfoDto is not null) { 
            
            rom.AddTitleName(titleInfoDto.Name!);
            if (rom.Version == null || rom.Version == "0" || int.Parse(rom.Version) < 0)
                rom.Version = titleInfoDto.Version;
            rom.Publisher = titleInfoDto.Publisher;
            rom.ProductCode = titleInfoDto.ProductCode;
            rom.Size = titleInfoDto.Size;
        }
        else
        {
            FileUtils.Log("rom not found in db");
        }
        return rom;
    }

   
}

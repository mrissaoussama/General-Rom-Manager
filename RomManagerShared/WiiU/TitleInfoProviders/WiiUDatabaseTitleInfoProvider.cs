﻿using RomManagerShared.Base;
using RomManagerShared.Base.Database;
using RomManagerShared.Interfaces;
using RomManagerShared.WiiU.Configuration;
using RomManagerShared.Utils;
using System.Text.Json;
namespace RomManagerShared.WiiU.TitleInfoProviders;

public class WiiUDatabaseTitleInfoProvider : TitleInfoProvider<WiiUConsole>
{
    public GenericRepository<WiiUWikiBrewTitleDTO> TitlesDatabase { get; set; }

    {
        TitlesDatabase = repo;
    }
    {

        var titleInfoDto =await TitlesDatabase.GetByPropertyAsync(nameof(WiiUWikiBrewTitleDTO.TitleID),rom.TitleID);
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
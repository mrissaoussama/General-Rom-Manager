using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.WiiU.Parsers;
using RomManagerShared.Utils;
using RomManagerShared.WiiU.Configuration;
namespace RomManagerShared.WiiU;

public class NintendoWiiUManager :ConsoleManager<WiiUConsole>
{
    public NintendoWiiUManager(TitleInfoProviderManager<WiiUConsole> titleInfoProviderManager,
RomParserExecutor<WiiUConsole> romParserExecutor)
: base(romParserExecutor)

    {
        TitleInfoProviderManager = titleInfoProviderManager;
        RomList = [];
    }
    public override async Task Setup()
    {
        List<Task> tasks =
        [ WiiUWikiBrewScraper.ScrapeTitles()
        //titleInfoProvider.LoadTitleDatabaseAsync(),
        // UpdateVersionProvider.LoadVersionDatabaseAsync()
        ];
        await Task.WhenAll(tasks);
    }

}


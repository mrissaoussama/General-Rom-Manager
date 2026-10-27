using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
using RomManagerShared.Wii.Configuration;
using RomManagerShared.Wii.Parsers;
using RomManagerShared.Wii.TitleInfoProviders;
namespace RomManagerShared.Wii;

public class WiiManager : ConsoleManager<WiiConsole>
{
    public WiiManager(TitleInfoProviderManager<WiiConsole> titleInfoProviderManager,
        RomParserExecutor<WiiConsole> romParserExecutor)
        : base(romParserExecutor)
    {
        TitleInfoProviderManager = titleInfoProviderManager;
    }


  
}
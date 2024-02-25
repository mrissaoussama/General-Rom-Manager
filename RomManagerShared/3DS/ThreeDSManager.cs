using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.ThreeDS.Configuration;
using RomManagerShared.ThreeDS.TitleInfoProviders;

namespace RomManagerShared.ThreeDS;
public class ThreeDSManager : ConsoleManager<ThreeDSConsole>
{
    public ThreeDSManager(TitleInfoProviderManager<ThreeDSConsole> titleInfoProviderManager,
        RomParserExecutor<ThreeDSConsole> romParserExecutor)
        : base(romParserExecutor)
    {
        TitleInfoProviderManager = titleInfoProviderManager;
    }
  
}

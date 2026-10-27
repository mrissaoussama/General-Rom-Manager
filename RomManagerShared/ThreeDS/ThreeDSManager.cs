using RomManagerShared.Base;
using RomManagerShared.Interfaces;

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

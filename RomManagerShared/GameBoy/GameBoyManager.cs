using RomManagerShared.Base;
using RomManagerShared.GameBoy.Parsers;
using RomManagerShared.Interfaces;
namespace RomManagerShared.GameBoy;

public class GameBoyManager : ConsoleManager<GameBoyConsole>
{
    public GameBoyManager(
     RomParserExecutor<GameBoyConsole> romParserExecutor)
     : base(romParserExecutor)
    {
    }
}

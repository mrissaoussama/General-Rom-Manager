using RomManagerShared.Base;
using RomManagerShared.GameBoyAdvance.Parsers;
using RomManagerShared.Interfaces;
namespace RomManagerShared.GameBoyAdvance;

public class GameBoyAdvanceManager : ConsoleManager<GameBoyAdvanceConsole>
{
    public GameBoyAdvanceManager(
   RomParserExecutor<GameBoyAdvanceConsole> romParserExecutor)
   : base(romParserExecutor)
    {
    }
   
}

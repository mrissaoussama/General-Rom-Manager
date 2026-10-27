using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.SegaSaturn.Parsers;
namespace RomManagerShared.SegaSaturn;

public class SegaSaturnManager : ConsoleManager<SegaSaturnConsole>
{
    public SegaSaturnManager(
RomParserExecutor<SegaSaturnConsole> romParserExecutor)
: base(romParserExecutor)
    {
    }
}

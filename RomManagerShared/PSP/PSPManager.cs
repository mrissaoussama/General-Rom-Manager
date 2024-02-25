using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.PSP.Parsers;
namespace RomManagerShared.PSP;

public class PSPManager : ConsoleManager<PSPConsole>
{
    public PSPManager(
RomParserExecutor<PSPConsole> romParserExecutor)
: base(romParserExecutor)
    {
    }
}

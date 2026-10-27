using RomManagerShared.Base;
using RomManagerShared.DS.Parsers;
using RomManagerShared.Interfaces;
namespace RomManagerShared.DS;

public class DSManager : ConsoleManager<DSConsole>
{

    public DSManager(
     RomParserExecutor<DSConsole> romParserExecutor)
     : base(romParserExecutor)
    {
    }

}

using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.PS3.Parsers;
namespace RomManagerShared.PS3;

public class PS3Manager : ConsoleManager<PS3Console>
{
 
    public PS3Manager(
RomParserExecutor<PS3Console> romParserExecutor)
: base(romParserExecutor)
    {
    }
}

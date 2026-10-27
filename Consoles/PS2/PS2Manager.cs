using RomManagerShared.Base;
using RomManagerShared.PS2.Parsers;
using RomManagerShared.Interfaces;
namespace RomManagerShared.PS2;

public class PS2Manager : ConsoleManager<PS2Console>
{
    public PS2Manager(
RomParserExecutor<PS2Console> romParserExecutor)
: base(romParserExecutor)
    {
    }
}

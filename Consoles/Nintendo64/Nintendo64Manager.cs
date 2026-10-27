using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Nintendo64.Parsers;
namespace RomManagerShared.Nintendo64;

public class Nintendo64Manager : ConsoleManager<N64Console>
{
    public Nintendo64Manager(
RomParserExecutor<N64Console> romParserExecutor)
: base(romParserExecutor)
    {
    }
}

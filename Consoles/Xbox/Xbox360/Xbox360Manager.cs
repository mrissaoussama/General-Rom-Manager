using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Xbox360.Parsers;
namespace RomManagerShared.Xbox360;

public class Xbox360Manager :ConsoleManager<Xbox360Console>
{
    public Xbox360Manager(
   RomParserExecutor<Xbox360Console> romParserExecutor)
   : base(romParserExecutor)
    {
    }
}

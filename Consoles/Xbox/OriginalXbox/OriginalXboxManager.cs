using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.OriginalXbox.Parsers;
namespace RomManagerShared.OriginalXbox;

public class OriginalXboxManager : ConsoleManager<OriginalXboxConsole>

{
    public OriginalXboxManager(
RomParserExecutor<OriginalXboxConsole> romParserExecutor)
: base(romParserExecutor)
    {
    }
}

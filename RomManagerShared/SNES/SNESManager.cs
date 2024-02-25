using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.SNES.Parsers;
namespace RomManagerShared.SNES;

public class SNESManager : ConsoleManager<SNESConsole>
{

    public SNESManager(
   RomParserExecutor<SNESConsole> romParserExecutor)
   : base(romParserExecutor)
    {
    }
    public static bool IsValidRom(string path)
    {
        return SNESUtils.IsSNESRom(path);
    }
}

using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.PSVita.Parsers;
namespace RomManagerShared.PSVita;

public class PSVitaManager : ConsoleManager<PSVitaConsole>
{
    public PSVitaManager(
RomParserExecutor<PSVitaConsole> romParserExecutor)
: base(romParserExecutor)
    {
    }
}

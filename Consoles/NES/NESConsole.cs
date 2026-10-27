using RomManagerShared.Interfaces;

namespace RomManagerShared.NES;

[ConsoleInfo("NES", "Nintendo", "NES")]
public class NESConsole : GamingConsole
{
    public NESConsole() { Name = "NES"; }
}

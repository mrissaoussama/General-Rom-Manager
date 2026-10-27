using RomManagerShared.Interfaces;

namespace RomManagerShared.SNES;

[ConsoleInfo("SNES", "Nintendo", "SNES")]
public class SNESConsole : GamingConsole
{
    public SNESConsole() { Name = "SNES"; }
}

using RomManagerShared.Interfaces;

namespace RomManagerShared.DS;

[ConsoleInfo("Nintendo DS", "Nintendo", "NDS")]
public class DSConsole : GamingConsole
{
    public DSConsole() { Name = "DS"; }
}

using RomManagerShared.Interfaces;

namespace RomManagerShared.Genesis;

[ConsoleInfo("Mega Drive / Genesis", "Sega", "MD")]
public class GenesisConsole : GamingConsole
{
    public GenesisConsole() { Name = "Genesis"; }
}

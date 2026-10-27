using RomManagerShared.Interfaces;

namespace RomManagerShared.GameBoyAdvance;

[ConsoleInfo("Game Boy Advance", "Nintendo", "GBA")]
public class GameBoyAdvanceConsole : GamingConsole
{
    public GameBoyAdvanceConsole() { Name = "GameBoy Advance"; }
}

using RomManagerShared.Interfaces;

namespace RomManagerShared.GameBoy;

[ConsoleInfo("Game Boy / GBC", "Nintendo", "GB")]
public class GameBoyConsole : GamingConsole
{
    public GameBoyConsole() { Name = "GameBoy"; }
}

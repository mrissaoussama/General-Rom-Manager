using RomManagerShared.Interfaces;

namespace RomManagerShared.GameCube;

[ConsoleInfo("GameCube", "Nintendo", "GC")]
public class GameCubeConsole : GamingConsole
{
    public GameCubeConsole() { Name = "GameCube"; }
}

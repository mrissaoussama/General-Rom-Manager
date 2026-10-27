using RomManagerShared.Interfaces;

namespace RomManagerShared.Switch;

[ConsoleInfo("Nintendo Switch", "Nintendo", "NSW")]
public class SwitchConsole : GamingConsole
{
    public SwitchConsole() { Name = "Switch"; }
}

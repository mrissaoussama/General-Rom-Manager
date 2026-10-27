using RomManagerShared.Interfaces;

namespace RomManagerShared.Xbox360;

[ConsoleInfo("Xbox 360", "Microsoft", "360")]
public class Xbox360Console : GamingConsole
{
    public Xbox360Console() { Name = "Xbox 360"; }
}

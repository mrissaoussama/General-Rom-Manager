using RomManagerShared.Interfaces;

namespace RomManagerShared.Nintendo64;

[ConsoleInfo("Nintendo 64", "Nintendo", "N64")]
public class N64Console : GamingConsole
{
    public N64Console() { Name = "Nintendo 64"; }
}

using RomManagerShared.Interfaces;

namespace RomManagerShared.PCEngine;

[ConsoleInfo("PC Engine / TurboGrafx-16", "NEC", "PCE")]
public class PCEngineConsole : GamingConsole
{
    public PCEngineConsole() { Name = "PC Engine"; }
}

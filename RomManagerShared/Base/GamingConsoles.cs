namespace RomManagerShared.Interfaces;

/// <summary>Base type for all gaming console marker classes.</summary>
public class GamingConsole
{
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
}

public class SwitchConsole : GamingConsole
{
    public SwitchConsole() { Name = "Switch"; }
}

public class ThreeDSConsole : GamingConsole
{
    public ThreeDSConsole() { Name = "3DS"; }
}

public class DSConsole : GamingConsole
{
    public DSConsole() { Name = "DS"; }
}

public class GameBoyConsole : GamingConsole
{
    public GameBoyConsole() { Name = "GameBoy"; }
}

public class GameBoyAdvanceConsole : GamingConsole
{
    public GameBoyAdvanceConsole() { Name = "GameBoy Advance"; }
}

public class PS2Console : GamingConsole
{
    public PS2Console() { Name = "PS2"; }
}

public class PS3Console : GamingConsole
{
    public PS3Console() { Name = "PS3"; }
}

public class PS4Console : GamingConsole
{
    public PS4Console() { Name = "PS4"; }
}

public class PSPConsole : GamingConsole
{
    public PSPConsole() { Name = "PSP"; }
}

public class PSVitaConsole : GamingConsole
{
    public PSVitaConsole() { Name = "PS Vita"; }
}

public class SNESConsole : GamingConsole
{
    public SNESConsole() { Name = "SNES"; }
}

public class WiiConsole : GamingConsole
{
    public WiiConsole() { Name = "Wii"; }
}

public class WiiUConsole : GamingConsole
{
    public WiiUConsole() { Name = "Wii U"; }
}

public class N64Console : GamingConsole
{
    public N64Console() { Name = "Nintendo 64"; }
}

public class SegaSaturnConsole : GamingConsole
{
    public SegaSaturnConsole() { Name = "Sega Saturn"; }
}

public class OriginalXboxConsole : GamingConsole
{
    public OriginalXboxConsole() { Name = "Original Xbox"; }
}

public class Xbox360Console : GamingConsole
{
    public Xbox360Console() { Name = "Xbox 360"; }
}

public class GameCubeConsole : GamingConsole
{
    public GameCubeConsole() { Name = "GameCube"; }
}

public class PS1Console : GamingConsole
{
    public PS1Console() { Name = "PS1"; }
}

public class NESConsole : GamingConsole
{
    public NESConsole() { Name = "NES"; }
}

public class GenesisConsole : GamingConsole
{
    public GenesisConsole() { Name = "Genesis"; }
}

public class MasterSystemConsole : GamingConsole
{
    public MasterSystemConsole() { Name = "Master System"; }
}

public class GameGearConsole : GamingConsole
{
    public GameGearConsole() { Name = "Game Gear"; }
}

public class PCEngineConsole : GamingConsole
{
    public PCEngineConsole() { Name = "PC Engine"; }
}

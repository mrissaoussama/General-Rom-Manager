using RomManagerShared.Base;

namespace RomManagerShared.MasterSystem;

/// <summary>Fields read from the TMR SEGA header (Master System and Game Gear).</summary>
public abstract class SegaTmrGame : Game
{
    [UiProperty(DisplayName = "Region Code")]
    public string? RegionCode { get; set; }

    [UiProperty(DisplayName = "Header")]
    public string? HeaderFormat { get; set; }

    public void Apply(SegaTmrHeader? header)
    {
        if (header is null) { HeaderFormat = "None"; return; }
        HeaderFormat = $"TMR SEGA @ 0x{header.Offset:X4}";
        RegionCode = header.RegionName;
        ProductCode = header.ProductCode;
        Version = header.Version.ToString();
        if (header.Regions.Count > 0) Regions = header.Regions;
    }
}

public class MasterSystemGame : SegaTmrGame;

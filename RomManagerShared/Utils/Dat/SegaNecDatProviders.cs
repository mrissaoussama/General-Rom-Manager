using RomManagerShared.Base;
using RomManagerShared.Genesis;
using RomManagerShared.Interfaces;
using RomManagerShared.MasterSystem;
using RomManagerShared.PCEngine;

using RomManagerShared.Utils;

namespace RomManagerShared.Utils.Dat;

public class GenesisDatProvider : DatTitleInfoProvider<GenesisConsole>
{
    protected override string? LibretroSystem(Rom rom) => "Sega - Mega Drive - Genesis";

    protected override bool MatchesDat(string f) => f.Contains("Mega Drive - Genesis", StringComparison.OrdinalIgnoreCase);

    // No-Intro hashes the plain image: strip copier headers and de-interleave SMD dumps.
    protected override IEnumerable<ReadOnlyMemory<byte>> Payloads(byte[] data)
    {
        yield return data;
        var layout = GenesisFormat.Detect(data, data.Length);
        if (layout != GenesisLayout.Raw) yield return GenesisFormat.Normalize(data, layout);
    }
}

public class MasterSystemDatProvider : DatTitleInfoProvider<MasterSystemConsole>
{
    protected override string? LibretroSystem(Rom rom) => "Sega - Master System - Mark III";

    protected override bool MatchesDat(string f) => f.Contains("Sega - Master System", StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<ReadOnlyMemory<byte>> Payloads(byte[] data)
    {
        yield return data;
        if (SegaTmrHeader.HasCopierHeader(data.Length)) yield return data.AsMemory(512);
    }
}

public class GameGearDatProvider : DatTitleInfoProvider<GameGearConsole>
{
    protected override string? LibretroSystem(Rom rom) => "Sega - Game Gear";

    protected override bool MatchesDat(string f) => f.Contains("Sega - Game Gear", StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<ReadOnlyMemory<byte>> Payloads(byte[] data)
    {
        yield return data;
        if (SegaTmrHeader.HasCopierHeader(data.Length)) yield return data.AsMemory(512);
    }
}

public class PCEngineDatProvider : DatTitleInfoProvider<PCEngineConsole>
{
    protected override string? LibretroSystem(Rom rom) => "NEC - PC Engine - TurboGrafx 16";

    // HuCard set only, not the CD or SuperGrafx DATs.
    protected override bool MatchesDat(string f) => f.Contains("PC Engine - TurboGrafx", StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<ReadOnlyMemory<byte>> Payloads(byte[] data)
    {
        yield return data;
        if (PCEngineGame.HasCopierHeader(data.Length)) yield return data.AsMemory(512);
    }
}

using RomManagerShared.Genesis;
using RomManagerShared.Interfaces;
using RomManagerShared.MasterSystem;
using RomManagerShared.PCEngine;

namespace RomManagerShared.Utils.Dat;

public class GenesisDatProvider : DatTitleInfoProvider<GenesisConsole>
{
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
    protected override bool MatchesDat(string f) => f.Contains("Sega - Master System", StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<ReadOnlyMemory<byte>> Payloads(byte[] data)
    {
        yield return data;
        if (SegaTmrHeader.HasCopierHeader(data.Length)) yield return data.AsMemory(512);
    }
}

public class GameGearDatProvider : DatTitleInfoProvider<GameGearConsole>
{
    protected override bool MatchesDat(string f) => f.Contains("Sega - Game Gear", StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<ReadOnlyMemory<byte>> Payloads(byte[] data)
    {
        yield return data;
        if (SegaTmrHeader.HasCopierHeader(data.Length)) yield return data.AsMemory(512);
    }
}

public class PCEngineDatProvider : DatTitleInfoProvider<PCEngineConsole>
{
    // HuCard set only, not the CD or SuperGrafx DATs.
    protected override bool MatchesDat(string f) => f.Contains("PC Engine - TurboGrafx", StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<ReadOnlyMemory<byte>> Payloads(byte[] data)
    {
        yield return data;
        if (PCEngineGame.HasCopierHeader(data.Length)) yield return data.AsMemory(512);
    }
}

using RomManagerShared.Base;
using RomManagerShared.Genesis;
using RomManagerShared.Interfaces;
using RomManagerShared.MasterSystem;
using RomManagerShared.PCEngine;
using RomManagerShared.Utils.Dat;
using RomManagerShared.Utils;

namespace RomManagerShared.Genesis.TitleInfoProviders;

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

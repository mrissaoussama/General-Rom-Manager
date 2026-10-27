using RomManagerShared.Base;
using RomManagerShared.Genesis;
using RomManagerShared.Interfaces;
using RomManagerShared.MasterSystem;
using RomManagerShared.PCEngine;
using RomManagerShared.Utils.Dat;
using RomManagerShared.Utils;

namespace RomManagerShared.PCEngine.TitleInfoProviders;

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

using RomManagerShared.Base;
using RomManagerShared.Genesis;
using RomManagerShared.Interfaces;
using RomManagerShared.MasterSystem;
using RomManagerShared.PCEngine;
using RomManagerShared.Utils.Dat;
using RomManagerShared.Utils;

namespace RomManagerShared.MasterSystem.TitleInfoProviders;

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

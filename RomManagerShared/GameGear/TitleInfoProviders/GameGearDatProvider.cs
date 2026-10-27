using RomManagerShared.Base;
using RomManagerShared.GameGear;
using RomManagerShared.Genesis;
using RomManagerShared.Interfaces;
using RomManagerShared.MasterSystem;
using RomManagerShared.PCEngine;
using RomManagerShared.Utils.Dat;
using RomManagerShared.Utils;

namespace RomManagerShared.GameGear.TitleInfoProviders;

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

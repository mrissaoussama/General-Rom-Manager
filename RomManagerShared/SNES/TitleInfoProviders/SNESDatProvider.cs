using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.SNES;
using RomManagerShared.Utils.Dat;
using RomManagerShared.Utils;

namespace RomManagerShared.SNES.TitleInfoProviders;

public class SNESDatProvider : DatTitleInfoProvider<SNESConsole>
{
    protected override bool MatchesDat(string f) => f.Contains("Super Nintendo Entertainment System", StringComparison.OrdinalIgnoreCase);

    protected override string? LibretroSystem(Rom rom) => "Nintendo - Super Nintendo Entertainment System";

    // Copier dumps carry a 512-byte header.
    protected override IEnumerable<ReadOnlyMemory<byte>> Payloads(byte[] data)
    {
        yield return data;
        if (data.Length % 1024 == 512) yield return data.AsMemory(512);
    }
}

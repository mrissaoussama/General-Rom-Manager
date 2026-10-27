using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.NES;
using RomManagerShared.Utils.Dat;
using RomManagerShared.Utils;

namespace RomManagerShared.NES.TitleInfoProviders;

public class NESDatProvider : DatTitleInfoProvider<NESConsole>
{
    protected override bool MatchesDat(string f) => f.Contains("Nintendo Entertainment System", StringComparison.OrdinalIgnoreCase)
        && !f.Contains("Super Nintendo", StringComparison.OrdinalIgnoreCase);

    protected override string? LibretroSystem(Rom rom) => "Nintendo - Nintendo Entertainment System";

    // Headered DATs hash the whole file, headerless ones skip the 16-byte iNES header.
    protected override IEnumerable<ReadOnlyMemory<byte>> Payloads(byte[] data)
    {
        yield return data;
        if (data.Length > 16 && data[0] == 'N' && data[1] == 'E' && data[2] == 'S' && data[3] == 0x1A)
            yield return data.AsMemory(16);
    }
}

using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Nintendo64;
using RomManagerShared.Utils.Dat;
using RomManagerShared.Utils;

namespace RomManagerShared.Nintendo64.TitleInfoProviders;

public class N64DatProvider : DatTitleInfoProvider<N64Console>
{
    protected override string? LibretroSystem(Rom rom) => "Nintendo - Nintendo 64";

    protected override bool MatchesDat(string f) => f.Contains("Nintendo 64", StringComparison.OrdinalIgnoreCase)
        && !f.Contains("64DD", StringComparison.OrdinalIgnoreCase);

    // No-Intro hashes big-endian (.z64); convert byte-swapped (.v64) and little-endian (.n64) dumps.
    protected override IEnumerable<ReadOnlyMemory<byte>> Payloads(byte[] data)
    {
        if (data.Length < 4 || data.Length % 4 != 0) { yield return data; yield break; }
        switch (data[0])
        {
            case 0x37: // v64: 16-bit swapped
                for (int i = 0; i < data.Length; i += 2) (data[i], data[i + 1]) = (data[i + 1], data[i]);
                break;
            case 0x40: // n64: 32-bit little-endian
                for (int i = 0; i < data.Length; i += 4)
                {
                    (data[i], data[i + 3]) = (data[i + 3], data[i]);
                    (data[i + 1], data[i + 2]) = (data[i + 2], data[i + 1]);
                }
                break;
        }
        yield return data;
    }
}

using RomManagerShared.Interfaces;

namespace RomManagerShared.Utils.Dat;

// DAT file names follow No-Intro's "<Company> - <System> (...).dat".

public class NESDatProvider : DatTitleInfoProvider<NESConsole>
{
    protected override bool MatchesDat(string f) => f.Contains("Nintendo Entertainment System", StringComparison.OrdinalIgnoreCase)
        && !f.Contains("Super Nintendo", StringComparison.OrdinalIgnoreCase);

    // Headered DATs hash the whole file, headerless ones skip the 16-byte iNES header.
    protected override IEnumerable<ReadOnlyMemory<byte>> Payloads(byte[] data)
    {
        yield return data;
        if (data.Length > 16 && data[0] == 'N' && data[1] == 'E' && data[2] == 'S' && data[3] == 0x1A)
            yield return data.AsMemory(16);
    }
}

public class SNESDatProvider : DatTitleInfoProvider<SNESConsole>
{
    protected override bool MatchesDat(string f) => f.Contains("Super Nintendo Entertainment System", StringComparison.OrdinalIgnoreCase);

    // Copier dumps carry a 512-byte header.
    protected override IEnumerable<ReadOnlyMemory<byte>> Payloads(byte[] data)
    {
        yield return data;
        if (data.Length % 1024 == 512) yield return data.AsMemory(512);
    }
}

public class GameBoyDatProvider : DatTitleInfoProvider<GameBoyConsole>
{
    // Covers "Game Boy" and "Game Boy Color", not "Game Boy Advance".
    protected override bool MatchesDat(string f) =>
        f.Contains("Nintendo - Game Boy", StringComparison.OrdinalIgnoreCase) && !f.Contains("Advance", StringComparison.OrdinalIgnoreCase);
}

public class GameBoyAdvanceDatProvider : DatTitleInfoProvider<GameBoyAdvanceConsole>
{
    protected override bool MatchesDat(string f) => f.Contains("Game Boy Advance", StringComparison.OrdinalIgnoreCase);
}

public class N64DatProvider : DatTitleInfoProvider<N64Console>
{
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

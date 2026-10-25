using System.Buffers.Binary;
using System.Text;
using RomManagerShared.Base;

namespace RomManagerShared.Utils;

public enum NintendoDiscKind { GameCube, Wii }

/// <summary>GameCube/Wii disc header (ID6 + title) from raw ISO/GCM or CISO images.</summary>
public sealed record NintendoDiscHeader(string GameId, string Title, NintendoDiscKind Kind, int DiscNumber, int Version)
{
    private const uint WiiMagic = 0x5D1C9EA3;
    private const uint GameCubeMagic = 0xC2339F3D;
    private const uint CisoMagic = 0x4F534943; // "CISO" little-endian
    private const int CisoHeaderSize = 0x8000;

    public Region Region => RegionFromGameId(GameId);

    /// <summary>Region from the 4th character of a Nintendo game ID (GAF<b>E</b>01, FAF<b>E</b>).</summary>
    public static Region RegionFromGameId(string gameId) => gameId.Length < 4 ? Region.Unknown : gameId[3] switch
    {
        'E' => Region.USA,
        'P' or 'X' or 'Y' => Region.Europe,
        'J' => Region.Japan,
        'K' => Region.Korea,
        'D' => Region.Germany,
        'F' => Region.France,
        'S' => Region.Spain,
        'I' => Region.Italy,
        'U' => Region.Australia,
        'W' => Region.Taiwan,
        _ => Region.Unknown,
    };

    public static NintendoDiscHeader? Read(string path)
    {
        using var fs = FileUtils.OpenRead(path);
        Span<byte> header = stackalloc byte[0x80];
        if (fs.ReadAtLeast(header, header.Length, throwOnEndOfStream: false) < header.Length) return null;

        // CISO: 0x8000 header with a block map; block 0 holds the disc header.
        if (BinaryPrimitives.ReadUInt32LittleEndian(header) == CisoMagic)
        {
            fs.Position = CisoHeaderSize;
            if (fs.ReadAtLeast(header, header.Length, throwOnEndOfStream: false) < header.Length) return null;
        }
        return Parse(header);
    }

    public static NintendoDiscHeader? Parse(ReadOnlySpan<byte> header)
    {
        NintendoDiscKind kind;
        if (BinaryPrimitives.ReadUInt32BigEndian(header[0x18..]) == WiiMagic) kind = NintendoDiscKind.Wii;
        else if (BinaryPrimitives.ReadUInt32BigEndian(header[0x1C..]) == GameCubeMagic) kind = NintendoDiscKind.GameCube;
        else return null;

        var id = Encoding.ASCII.GetString(header[..6]);
        if (!id.All(char.IsAsciiLetterOrDigit)) return null;
        var title = Encoding.UTF8.GetString(header.Slice(0x20, 0x60)).TrimEnd('\0', ' ');
        return new NintendoDiscHeader(id, title, kind, header[6] + 1, header[7]);
    }
}

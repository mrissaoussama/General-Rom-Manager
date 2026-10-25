using System.Buffers.Binary;
using System.Text;
using RomManagerShared.Base;

namespace RomManagerShared.Utils;

public enum NintendoDiscKind { GameCube, Wii }

/// <summary>GameCube/Wii disc header (ID6 + title) from raw ISO/GCM, CISO or WIA/RVZ images.</summary>
public sealed record NintendoDiscHeader(string GameId, string Title, NintendoDiscKind Kind, int DiscNumber, int Version)
{
    private const uint WiiMagic = 0x5D1C9EA3;
    private const uint GameCubeMagic = 0xC2339F3D;
    private const uint CisoMagic = 0x4F534943; // "CISO" little-endian
    private const int CisoHeaderSize = 0x8000;
    private const uint WiaMagic = 0x01414957; // "WIA\x1" little-endian
    private const uint RvzMagic = 0x015A5652; // "RVZ\x1" little-endian
    private const int WiaIsoSizeOffset = 0x24;
    private const int WiaDiscTypeOffset = 0x48;
    private const int WiaDheadOffset = 0x58;

    public Region Region => RegionFromGameId(GameId);

    /// <summary>Uncompressed disc size when the container records it (WIA/RVZ), else null.</summary>
    public long? IsoSize { get; init; }

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
        var magic = BinaryPrimitives.ReadUInt32LittleEndian(header);
        if (magic == CisoMagic)
        {
            fs.Position = CisoHeaderSize;
            if (fs.ReadAtLeast(header, header.Length, throwOnEndOfStream: false) < header.Length) return null;
        }
        // WIA/RVZ (Dolphin): wia_file_head_t (0x48) + wia_disc_t, whose dhead holds the first 0x80
        // disc bytes uncompressed. Big-endian fields.
        else if (magic is WiaMagic or RvzMagic)
        {
            Span<byte> wia = stackalloc byte[WiaDheadOffset + 0x80];
            fs.Position = 0;
            if (fs.ReadAtLeast(wia, wia.Length, throwOnEndOfStream: false) < wia.Length) return null;
            var disc = Parse(wia.Slice(WiaDheadOffset, 0x80));
            var discType = BinaryPrimitives.ReadUInt32BigEndian(wia[WiaDiscTypeOffset..]);
            // disc_type must agree with the magic inside dhead (1 = GameCube, 2 = Wii).
            if (disc is null || discType != (disc.Kind == NintendoDiscKind.GameCube ? 1u : 2u)) return null;
            return disc with { IsoSize = (long)BinaryPrimitives.ReadUInt64BigEndian(wia[WiaIsoSizeOffset..]) };
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

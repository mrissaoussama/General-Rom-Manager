using System.Buffers.Binary;
using System.IO.Compression;
using DiscUtils.Iso9660;
using FluentAssertions;
using RomManagerShared.PSP.Parsers;
using RomManagerShared.Utils;

namespace RomManagerTests;

public class CsoStreamTests
{
    // CSO v1 writer: deflate each 2 KB block, keep it raw when compression doesn't help.
    private static byte[] ToCso(byte[] iso, int blockSize = 0x800)
    {
        var blocks = (iso.Length + blockSize - 1) / blockSize;
        var indexOffset = 0x18;
        var dataStart = indexOffset + (blocks + 1) * 4;
        var data = new MemoryStream();
        var index = new uint[blocks + 1];
        for (int b = 0; b < blocks; b++)
        {
            index[b] = (uint)(dataStart + data.Length);
            var chunk = iso.AsSpan(b * blockSize, Math.Min(blockSize, iso.Length - b * blockSize)).ToArray();
            var packed = new MemoryStream();
            using (var deflate = new DeflateStream(packed, CompressionLevel.Optimal, leaveOpen: true))
                deflate.Write(chunk);
            if (packed.Length < chunk.Length) data.Write(packed.ToArray());
            else { index[b] |= 0x80000000; data.Write(chunk); }
        }
        index[blocks] = (uint)(dataStart + data.Length);

        var cso = new byte[dataStart + data.Length];
        var s = cso.AsSpan();
        BinaryPrimitives.WriteUInt32LittleEndian(s, 0x4F534943);
        BinaryPrimitives.WriteUInt32LittleEndian(s[4..], 0x18);
        BinaryPrimitives.WriteUInt64LittleEndian(s[8..], (ulong)iso.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(s[0x10..], (uint)blockSize);
        s[0x14] = 1;
        for (int i = 0; i < index.Length; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(s[(indexOffset + i * 4)..], index[i]);
        data.ToArray().CopyTo(cso, dataStart);
        return cso;
    }

    [Fact]
    public void CsoStream_RoundTripsArbitraryReads()
    {
        var iso = new byte[0x800 * 5 + 100];
        new Random(42).NextBytes(iso.AsSpan(0, 0x800));        // incompressible block (stored raw)
        Array.Fill(iso, (byte)0xAB, 0x800, iso.Length - 0x800); // compressible blocks

        using var cso = CsoStream.TryOpen(new MemoryStream(ToCso(iso)))!;
        cso.Length.Should().Be(iso.Length);

        var buffer = new byte[3000];
        cso.Position = 0x700; // spans a raw and a compressed block
        cso.ReadAtLeast(buffer, buffer.Length).Should().Be(buffer.Length);
        buffer.Should().Equal(iso.AsSpan(0x700, 3000).ToArray());
    }

    [Fact]
    public void TryOpen_GameCubeCiso_ReturnsNull()
    {
        var gc = new byte[0x8000];
        BinaryPrimitives.WriteUInt32LittleEndian(gc, 0x4F534943);
        BinaryPrimitives.WriteUInt32LittleEndian(gc.AsSpan(4), 0x200000); // GC CISO: block size here

        CsoStream.TryOpen(new MemoryStream(gc)).Should().BeNull();
    }

    [Fact]
    public async Task PSPRomParser_ReadsCso()
    {
        var builder = new CDBuilder { UseJoliet = true };
        builder.AddFile(@"PSP_GAME\PARAM.SFO", SfoTestData.Build(("DISC_ID", "ULES00590"), ("TITLE", "Aces of War")));
        using var isoStream = new MemoryStream();
        builder.Build(isoStream);
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.cso");
        File.WriteAllBytes(path, ToCso(isoStream.ToArray()));
        try
        {
            (await new PSPRomParser().ProcessFile(path)).Should().ContainSingle()
                .Which.TitleID.Should().Be("ULES00590");
        }
        finally
        {
            File.Delete(path);
        }
    }

    // "abcd" then a 2044-byte overlapping match at offset 4: 2048 bytes of "abcd" repeated.
    private static byte[] Lz4AbcdBlock() =>
        [0x4F, (byte)'a', (byte)'b', (byte)'c', (byte)'d', 0x04, 0x00, 255, 255, 255, 255, 255, 255, 255, 240];

    [Fact]
    public void Lz4DecodeBlock_HandlesLongOverlappingMatches()
    {
        var output = new byte[2048];

        CsoStream.Lz4DecodeBlock(Lz4AbcdBlock(), output).Should().Be(2048);

        for (int i = 0; i < output.Length; i++) output[i].Should().Be((byte)"abcd"[i % 4]);
    }

    [Fact]
    public void ZsoStream_ReadsRawAndLz4Blocks()
    {
        var raw = new byte[0x800];
        new Random(7).NextBytes(raw);
        var lz4 = Lz4AbcdBlock();
        var dataStart = 0x18 + 3 * 4;
        var zso = new byte[dataStart + raw.Length + lz4.Length];
        var s = zso.AsSpan();
        BinaryPrimitives.WriteUInt32LittleEndian(s, 0x4F53495A); // ZISO
        BinaryPrimitives.WriteUInt32LittleEndian(s[4..], 0x18);
        BinaryPrimitives.WriteUInt64LittleEndian(s[8..], 0x1000);
        BinaryPrimitives.WriteUInt32LittleEndian(s[0x10..], 0x800);
        s[0x14] = 1;
        BinaryPrimitives.WriteUInt32LittleEndian(s[0x18..], (uint)dataStart | 0x80000000);
        BinaryPrimitives.WriteUInt32LittleEndian(s[0x1C..], (uint)(dataStart + raw.Length));
        BinaryPrimitives.WriteUInt32LittleEndian(s[0x20..], (uint)zso.Length);
        raw.CopyTo(zso, dataStart);
        lz4.CopyTo(zso, dataStart + raw.Length);

        using var stream = CsoStream.TryOpen(new MemoryStream(zso))!;
        var all = new byte[0x1000];
        stream.ReadExactly(all);

        all.AsSpan(0, 0x800).ToArray().Should().Equal(raw);
        all[0x800].Should().Be((byte)'a');
        all[0xFFF].Should().Be((byte)'d');
    }
}

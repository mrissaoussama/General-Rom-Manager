using System.Text;
using AwesomeAssertions;
using RomManagerShared.Base;
using RomManagerShared.Dreamcast;
using RomManagerShared.Dreamcast.Parsers;

namespace RomManagerTests;

public sealed class DreamcastDiscParserTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;

    public void Dispose() => Directory.Delete(_dir, true);

    private static byte[] BuildIpBin(string areas = "JUE", string device = "E6A1 GD-ROM2/2")
    {
        var ip = new byte[0x8000];
        ip.AsSpan().Fill((byte)' ');
        void Put(int offset, string value) => Encoding.ASCII.GetBytes(value).CopyTo(ip, offset);
        Put(0x00, "SEGA SEGAKATANA ");
        Put(0x10, "SEGA ENTERPRISES");
        Put(0x20, device);
        Put(0x30, areas);
        Put(0x38, "0799A10");
        Put(0x40, "MK-51000");
        Put(0x4A, "V1.005");
        Put(0x50, "19990908");
        Put(0x60, "1ST_READ.BIN");
        Put(0x70, "SEGA LC-T-123");
        Put(0x80, "SONIC ADVENTURE");
        return ip;
    }

    // Wraps 2048-byte sectors as raw Mode 1 (sync + header + data + EDC/ECC padding).
    private static byte[] ToRawMode1(byte[] data)
    {
        byte[] sync = [0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00];
        using var raw = new MemoryStream();
        for (int i = 0; i < data.Length; i += 2048)
        {
            raw.Write(sync);
            raw.Write([0, 2, 0, 1]);
            raw.Write(data, i, 2048);
            raw.Write(new byte[288]);
        }
        return raw.ToArray();
    }

    private string WriteGdi(string track3Name, byte[] track3, int sectorSize, bool quoted = false)
    {
        File.WriteAllBytes(Path.Combine(_dir, "track01.bin"), new byte[2352 * 4]);
        File.WriteAllBytes(Path.Combine(_dir, "track02.raw"), new byte[2352 * 4]);
        File.WriteAllBytes(Path.Combine(_dir, track3Name), track3);
        var name = quoted ? $"\"{track3Name}\"" : track3Name;
        var gdi = Path.Combine(_dir, "disc.gdi");
        File.WriteAllText(gdi, $"3\r\n1 0 4 2352 track01.bin 0\r\n2 756 0 2352 track02.raw 0\r\n3 45000 4 {sectorSize} {name} 0\r\n");
        return gdi;
    }

    [Fact]
    public async Task ProcessFile_GdiWithCookedTrack_ReadsIpBin()
    {
        var gdi = WriteGdi("track03.iso", BuildIpBin(), 2048);

        var rom = (await new DreamcastDiscParser().ProcessFile(gdi)).Should().ContainSingle().Subject;
        Golden.Verify("dreamcast-gdi", [rom], _dir);

        rom.TitleID.Should().Be("MK-51000");
        rom.ProductCode.Should().Be("MK-51000");
        rom.Version.Should().Be("V1.005");
        rom.Publisher.Should().Be("SEGA LC-T-123");
        rom.ReleaseDate.Should().Be(new DateOnly(1999, 9, 8));
        rom.Titles!.Single().Value.Should().Be("SONIC ADVENTURE");
        rom.Regions.Should().Equal(Region.Japan, Region.USA, Region.Europe);
        rom.Should().BeOfType<DreamcastGame>().Which.DiscNumber.Should().Be(2);
        rom.Path.Should().Be(gdi);
        rom.Size.Should().Be(2352 * 8 + 0x8000);
    }

    [Fact]
    public async Task ProcessFile_GdiWithRawTrackAndQuotedName_ReadsIpBin()
    {
        var gdi = WriteGdi("Game (Track 3).bin", ToRawMode1(BuildIpBin(areas: "U")), 2352, quoted: true);

        var rom = (await new DreamcastDiscParser().ProcessFile(gdi)).Should().ContainSingle().Subject;

        rom.TitleID.Should().Be("MK-51000");
        rom.Regions.Should().Equal(Region.USA);
    }

    [Fact]
    public async Task ProcessFile_GdiWithoutIpBin_ReturnsEmpty()
    {
        var gdi = WriteGdi("track03.bin", new byte[2352 * 16], 2352);

        (await new DreamcastDiscParser().ProcessFile(gdi)).Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessFile_GdiWithMissingTrack_ReturnsEmpty()
    {
        var gdi = Path.Combine(_dir, "missing.gdi");
        File.WriteAllText(gdi, "1\n3 45000 4 2352 nope.bin 0\n");

        (await new DreamcastDiscParser().ProcessFile(gdi)).Should().BeEmpty();
    }

    // CHD frames: 2448 bytes with raw Mode 1 data at offset 16.
    [Theory]
    [InlineData(2448, 16)]
    [InlineData(2448, 0)]
    [InlineData(2352, 16)]
    [InlineData(2048, 0)]
    public void Read_FindsHeaderInFrameLayouts(int frame, int offset)
    {
        var image = new byte[frame * 4];
        BuildIpBin().AsSpan(0, 2048).CopyTo(image.AsSpan(frame * 2 + offset));

        var ip = DreamcastIpBin.Read(new MemoryStream(image), frame, 2);

        ip.Should().NotBeNull();
        ip!.ProductNumber.Should().Be("MK-51000");
        DreamcastIpBin.Read(new MemoryStream(image), frame, 1).Should().BeNull();
    }
}

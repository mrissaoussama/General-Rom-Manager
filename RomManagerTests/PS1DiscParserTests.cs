using System.Text;
using DiscUtils.Iso9660;
using AwesomeAssertions;
using RomManagerShared.Base;
using RomManagerShared.PS1.Parsers;

namespace RomManagerTests;

public sealed class PS1DiscParserTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;

    public void Dispose() => Directory.Delete(_dir, true);

    private static byte[] BuildIso(string systemCnf)
    {
        var builder = new CDBuilder { UseJoliet = false };
        builder.AddFile("SYSTEM.CNF", Encoding.ASCII.GetBytes(systemCnf));
        builder.AddFile("SLUS_004.99", [1, 2, 3]);
        using var ms = new MemoryStream();
        builder.Build(ms);
        return ms.ToArray();
    }

    // Wraps 2048-byte sectors as raw Mode 2 Form 1 (sync + header + subheader + data + EDC/ECC padding).
    private static byte[] ToRawMode2(byte[] iso)
    {
        byte[] sync = [0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00];
        using var raw = new MemoryStream();
        for (int i = 0; i < iso.Length; i += 2048)
        {
            raw.Write(sync);
            raw.Write([0, 2, 0, 2]); // MSF (unused) + mode 2
            raw.Write(new byte[8]);  // subheader
            raw.Write(iso, i, 2048);
            raw.Write(new byte[280]);
        }
        return raw.ToArray();
    }

    [Fact]
    public async Task ProcessFile_CueWithRawBin_ReadsSerial()
    {
        File.WriteAllBytes(Path.Combine(_dir, "Game (Track 01).bin"), ToRawMode2(BuildIso("BOOT = cdrom:\\SLUS_004.99;1\r\n")));
        var cue = Path.Combine(_dir, "Game.cue");
        File.WriteAllText(cue, "FILE \"Game (Track 01).bin\" BINARY\n  TRACK 01 MODE2/2352\n    INDEX 01 00:00:00\n");

        var rom = (await new PS1DiscParser().ProcessFile(cue)).Should().ContainSingle().Subject;
        Golden.Verify("ps1-cue", [rom], _dir);

        rom.TitleID.Should().Be("SLUS_004.99");
        rom.Regions.Should().Equal(Region.USA);
        rom.Path.Should().Be(cue);
    }

    [Fact]
    public async Task ProcessFile_PlainIso_ReadsSerial()
    {
        var iso = Path.Combine(_dir, "game.iso");
        File.WriteAllBytes(iso, BuildIso("BOOT=cdrom:SCES_012.37;1"));

        (await new PS1DiscParser().ProcessFile(iso)).Should().ContainSingle()
            .Which.TitleID.Should().Be("SCES_012.37");
    }

    // CHD stores CD frames as 2448 bytes (sector + subcode); cooked MODE1 data sits at offset 0.
    [Theory]
    [InlineData(2448, 0)]
    [InlineData(2448, 24)]
    [InlineData(2352, 16)]
    public void RawCdReader_DetectsFrameLayouts(int frame, int offset)
    {
        var iso = BuildIso("BOOT2 = cdrom0:\\SLUS_217.55;1");
        var image = new byte[iso.Length / 2048 * frame];
        for (int i = 0; i < iso.Length / 2048; i++)
            Array.Copy(iso, i * 2048, image, i * frame + offset, 2048);

        var reader = new RomManagerShared.Utils.RawCdReader(new MemoryStream(image));

        reader.ReadBootExecutable().Should().Be("SLUS_217.55");
        reader.RootFileNames().Should().Contain("SLUS_004.99");
    }

    [Fact]
    public async Task ProcessFile_CueWithMissingBin_ReturnsEmpty()
    {
        var cue = Path.Combine(_dir, "Missing.cue");
        File.WriteAllText(cue, "FILE \"nope.bin\" BINARY\n");

        (await new PS1DiscParser().ProcessFile(cue)).Should().BeEmpty();
    }
}

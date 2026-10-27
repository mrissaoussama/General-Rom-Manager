using System.Buffers.Binary;
using System.Text;
using AwesomeAssertions;
using RomManagerShared.Base;
using RomManagerShared.PS4;
using RomManagerShared.PS4.Parsers;
using RomManagerShared.PS3;
using RomManagerShared.PSVita;
using RomManagerShared.Utils;

namespace RomManagerTests;

public sealed class PS4PkgTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("ps4").FullName;

    public void Dispose() => Directory.Delete(_dir, true);

    private static readonly byte[] Png = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];

    private static byte[] Pkg(byte[] sfo, bool encryptPic0 = true)
    {
        var pkg = new byte[0x2000];
        BinaryPrimitives.WriteUInt32BigEndian(pkg, 0x7F434E54);
        BinaryPrimitives.WriteUInt32BigEndian(pkg.AsSpan(0x10), 3);     // entry count
        BinaryPrimitives.WriteUInt32BigEndian(pkg.AsSpan(0x18), 0x100); // entry table
        Encoding.ASCII.GetBytes("UP0001-CUSA00001_00-GAME000000000001").CopyTo(pkg, 0x40);

        void Entry(int index, uint id, uint flags, int offset, byte[] data)
        {
            var e = pkg.AsSpan(0x100 + index * 32, 32);
            BinaryPrimitives.WriteUInt32BigEndian(e, id);
            BinaryPrimitives.WriteUInt32BigEndian(e[8..], flags);
            BinaryPrimitives.WriteUInt32BigEndian(e[16..], (uint)offset);
            BinaryPrimitives.WriteUInt32BigEndian(e[20..], (uint)data.Length);
            data.CopyTo(pkg, offset);
        }
        Entry(0, 0x1000, 0, 0x400, sfo);
        Entry(1, 0x1200, 0, 0x1000, Png);
        Entry(2, 0x1220, encryptPic0 ? 0x80000000 : 0, 0x1100, Png);
        return pkg;
    }

    private static byte[] Ps4Sfo(string category) => SfoTestData.Build(
        ("APP_TYPE", 1), ("APP_VER", "01.05"), ("CATEGORY", category), ("CONTENT_ID", "UP0001-CUSA00001_00-GAME000000000001"),
        ("SYSTEM_VER", 0x05050000), ("TITLE", "Test Game"), ("TITLE_ID", "CUSA00001"));

    [Fact]
    public void Read_ReturnsPlaintextEntriesAndSkipsEncryptedOnes()
    {
        var pkg = PS4PkgReader.Read(new MemoryStream(Pkg(Ps4Sfo("gd"))));

        pkg.ContentId.Should().Be("UP0001-CUSA00001_00-GAME000000000001");
        pkg.Sfo!.TitleId.Should().Be("CUSA00001");
        pkg.Sfo.Platform.Should().Be(SfoPlatform.PS4);
        pkg.Sfo.MinimumFirmware.Should().Be("5.05");
        pkg.Icon0.Should().Equal(Png);
        pkg.Pic0.Should().BeNull();
    }

    [Theory]
    [InlineData("gd", typeof(PS4Game))]
    [InlineData("gp", typeof(PS4Update))]
    [InlineData("ac", typeof(PS4DLC))]
    public async Task Parser_MapsCategoryToRomType(string category, Type expected)
    {
        var path = Path.Combine(_dir, $"{category}.pkg");
        File.WriteAllBytes(path, Pkg(Ps4Sfo(category)));

        var rom = (await new PS4PKGParser().ProcessFile(path)).Should().ContainSingle().Which;
        Golden.Verify($"ps4-pkg-{category}", [rom], _dir);

        rom.Should().BeOfType(expected);
        rom.TitleID.Should().Be("CUSA00001");
        rom.Version.Should().Be("01.05");
        rom.Titles![0].Value.Should().Be("Test Game");
    }

    [Fact]
    public async Task Parser_IgnoresNonPS4Pkgs()
    {
        var path = Path.Combine(_dir, "ps3.pkg");
        File.WriteAllBytes(path, [0x7F, (byte)'P', (byte)'K', (byte)'G', .. new byte[0x1000]]);

        (await new PS4PKGParser().ProcessFile(path)).Should().BeEmpty();
    }

    [Fact]
    public void SfoReaders_DetectPlatformAndFirmware()
    {
        var ps3 = ParamSfo.Read(SfoTestData.Build(("CATEGORY", "DG"), ("PS3_SYSTEM_VER", "04.4000"), ("TITLE_ID", "BLUS30001")));
        ps3.Platform.Should().Be(SfoPlatform.PS3);
        ps3.MinimumFirmware.Should().Be("4.40");
        PS3SFOReader.ParseSFO(SfoTestData.Build(("CATEGORY", "DG"), ("PS3_SYSTEM_VER", "04.4000"), ("TITLE_ID", "BLUS30001")))
            .Should().BeOfType<RomManagerShared.PS3.PS3Game>();

        var vita = SfoTestData.Build(("CATEGORY", "gd"), ("PSP2_SYSTEM_VER", 0x03600000), ("TITLE_ID", "PCSE00001"), ("TITLE", "Vita"));
        ParamSfo.Read(vita).Platform.Should().Be(SfoPlatform.PSVita);
        var rom = PSVitaSFOReader.ParseSFO(vita);
        rom.Should().BeOfType<RomManagerShared.PSVita.PSVitaGame>();
        rom!.MinimumFirmware.Should().Be("3.60");
    }
}

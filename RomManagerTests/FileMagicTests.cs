using System.Text;
using AwesomeAssertions;
using RomManagerShared.Utils;

namespace RomManagerTests;

public class FileMagicTests
{
    private static byte[] Iso(string systemId)
    {
        var h = new byte[FileMagic.HeaderLength];
        h[0x8000] = 1;
        Encoding.ASCII.GetBytes("CD001").CopyTo(h, 0x8001);
        Encoding.ASCII.GetBytes(systemId.PadRight(32)).CopyTo(h, 0x8008);
        return h;
    }

    [Fact]
    public void Packages_AreToldApart()
    {
        byte[] pkg3 = [0x7F, (byte)'P', (byte)'K', (byte)'G', 0, 0];
        byte[] ps4 = [0x7F, (byte)'C', (byte)'N', (byte)'T', 0, 0];

        FileMagic.IsPkg3(pkg3).Should().BeTrue();
        FileMagic.IsPs4Pkg(pkg3).Should().BeFalse();
        FileMagic.IsPs4Pkg(ps4).Should().BeTrue();
        FileMagic.IsPkg3(ps4).Should().BeFalse();
    }

    [Fact]
    public void NintendoDiscs_AreRecognisedByTheirMagic()
    {
        var gc = new byte[0x40];
        gc[0x1C] = 0xC2; gc[0x1D] = 0x33; gc[0x1E] = 0x9F; gc[0x1F] = 0x3D;
        var wii = new byte[0x40];
        wii[0x18] = 0x5D; wii[0x19] = 0x1C; wii[0x1A] = 0x9E; wii[0x1B] = 0xA3;

        FileMagic.IsGameCubeDisc(gc).Should().BeTrue();
        FileMagic.IsWiiDisc(gc).Should().BeFalse();
        FileMagic.IsWiiDisc(wii).Should().BeTrue();
        FileMagic.IsGameCubeDisc(wii).Should().BeFalse();
    }

    [Fact]
    public void IsoSystemId_ReadsThePrimaryVolumeDescriptor()
    {
        FileMagic.IsoSystemId(Iso("PSP GAME")).Should().Be("PSP GAME");
        FileMagic.IsoSystemId(Iso("PLAYSTATION")).Should().Be("PLAYSTATION");
        FileMagic.IsoSystemId(new byte[FileMagic.HeaderLength]).Should().BeNull();
        FileMagic.IsoSystemId([1, 2, 3]).Should().BeNull();
    }

    [Fact]
    public void DiscParsers_RuleOutIsosOfOtherSystems()
    {
        var psp = new RomManagerShared.PSP.Parsers.PSPRomParser();
        var ps1 = new RomManagerShared.PS1.Parsers.PS1DiscParser();
        var gc = new RomManagerShared.GameCube.Parsers.GameCubeDiscParser();

        psp.CanParse("a.iso", Iso("PSP GAME")).Should().BeTrue();
        psp.CanParse("a.iso", Iso("PLAYSTATION")).Should().BeFalse();
        psp.CanParse("a.cso", Iso("PLAYSTATION")).Should().BeTrue(); // containers are checked when opened
        ps1.CanParse("a.iso", Iso("PLAYSTATION")).Should().BeTrue();
        ps1.CanParse("a.iso", Iso("PSP GAME")).Should().BeFalse();
        ps1.CanParse("a.iso", new byte[100]).Should().BeTrue(); // raw 2352-byte image: still tried
        gc.CanParse("a.iso", Iso("PSP GAME")).Should().BeFalse();
        gc.CanParse("a.rvz", new byte[100]).Should().BeTrue();
    }
}

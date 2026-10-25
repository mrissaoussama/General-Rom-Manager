using System.Text;
using DiscUtils.Iso9660;
using FluentAssertions;
using RomManagerShared.Base;
using RomManagerShared.PS2.Parsers;

namespace RomManagerTests;

public class PS2RomParserTests
{
    private static string BuildIso(params (string Name, string Content)[] files)
    {
        var builder = new CDBuilder { UseJoliet = false, VolumeIdentifier = "TEST" };
        foreach (var (name, content) in files)
            builder.AddFile(name, Encoding.ASCII.GetBytes(content));
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.iso");
        builder.Build(path);
        return path;
    }

    [Fact]
    public async Task ProcessFile_UsesBootEntryFromSystemCnf()
    {
        var iso = BuildIso(
            ("SYSTEM.CNF", "BOOT2 = cdrom0:\\SLUS_200.90;1\r\nVER = 1.00\r\n"),
            ("SLUS_200.90", "elf"),
            ("SLES_500.09", "other"));
        try
        {
            var roms = await new PS2RomParser().ProcessFile(iso);

            roms.Should().ContainSingle();
            roms[0].TitleID.Should().Be("SLUS_200.90");
            roms[0].Regions.Should().Equal(Region.USA);
        }
        finally
        {
            File.Delete(iso);
        }
    }

    [Fact]
    public async Task ProcessFile_WithoutSystemCnf_FallsBackToRootScan()
    {
        var iso = BuildIso(("SCES_123.45", "elf"));
        try
        {
            var roms = await new PS2RomParser().ProcessFile(iso);

            roms.Should().ContainSingle().Which.TitleID.Should().Be("SCES_123.45");
        }
        finally
        {
            File.Delete(iso);
        }
    }
}

using DiscUtils.Iso9660;
using FluentAssertions;
using RomManagerShared.Base;
using RomManagerShared.PSP;
using RomManagerShared.PSP.Parsers;
using RomManagerShared.Utils;

namespace RomManagerTests;

public class ParamSfoTests
{
    [Fact]
    public void Read_ParsesStringsAndInts()
    {
        var bytes = SfoTestData.Build(("TITLE", "Aces of War"), ("DISC_NUMBER", 1), ("DISC_ID", "ULES00590"));

        var sfo = ParamSfo.Read(bytes);

        sfo.GetString("TITLE").Should().Be("Aces of War");
        sfo.GetString("DISC_ID").Should().Be("ULES00590");
        sfo.GetInt("DISC_NUMBER").Should().Be(1);
        sfo.GetString("MISSING").Should().BeNull();
    }

    [Fact]
    public void Read_RejectsBadMagic()
    {
        var act = () => ParamSfo.Read(new byte[0x20]);
        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public async Task PSPRomParser_ReadsSfoFromIso()
    {
        var sfo = SfoTestData.Build(
            ("CATEGORY", "UG"), ("DISC_ID", "ULES00590"), ("DISC_VERSION", "1.01"),
            ("PSP_SYSTEM_VER", "3.71"), ("TITLE", "Aces of War"), ("DISC_NUMBER", 1), ("DISC_TOTAL", 1));
        var builder = new CDBuilder { UseJoliet = true, VolumeIdentifier = "PSP" };
        builder.AddFile(@"PSP_GAME\PARAM.SFO", sfo);
        var iso = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.iso");
        builder.Build(iso);
        try
        {
            var roms = await new PSPRomParser().ProcessFile(iso);

            var game = roms.Should().ContainSingle().Which.Should().BeOfType<PSPGame>().Subject;
            game.TitleID.Should().Be("ULES00590");
            game.Version.Should().Be("1.01");
            game.MinimumFirmware.Should().Be("3.71");
            game.Titles![0].Value.Should().Be("Aces of War");
            game.Regions.Should().Equal(Region.Europe);
            game.Path.Should().Be(iso);
        }
        finally
        {
            File.Delete(iso);
        }
    }

    [Fact]
    public async Task PSPRomParser_NonPspIso_ReturnsEmpty()
    {
        var builder = new CDBuilder();
        builder.AddFile("README.TXT", [1]);
        var iso = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.iso");
        builder.Build(iso);
        try
        {
            (await new PSPRomParser().ProcessFile(iso)).Should().BeEmpty();
        }
        finally
        {
            File.Delete(iso);
        }
    }
}

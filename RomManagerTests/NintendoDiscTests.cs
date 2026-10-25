using System.Buffers.Binary;
using System.Text;
using FluentAssertions;
using RomManagerShared.Base;
using RomManagerShared.GameCube;
using RomManagerShared.GameCube.Parsers;
using RomManagerShared.Utils;
using RomManagerShared.Wii.Parsers;

namespace RomManagerTests;

public class NintendoDiscTests
{
    private static byte[] DiscHeader(string id, string title, bool wii)
    {
        var h = new byte[0x440];
        Encoding.ASCII.GetBytes(id).CopyTo(h, 0);
        if (wii) BinaryPrimitives.WriteUInt32BigEndian(h.AsSpan(0x18), 0x5D1C9EA3);
        else BinaryPrimitives.WriteUInt32BigEndian(h.AsSpan(0x1C), 0xC2339F3D);
        Encoding.ASCII.GetBytes(title).CopyTo(h, 0x20);
        return h;
    }

    private static string Write(string ext, byte[] data)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.{ext}");
        File.WriteAllBytes(path, data);
        return path;
    }

    [Fact]
    public async Task GameCubeParser_ReadsIso()
    {
        var path = Write("iso", DiscHeader("GAFE01", "AnimalCrossing", wii: false));
        try
        {
            var game = (await new GameCubeDiscParser().ProcessFile(path)).Should().ContainSingle()
                .Which.Should().BeOfType<GameCubeGame>().Subject;
            game.TitleID.Should().Be("GAFE01");
            game.Titles![0].Value.Should().Be("AnimalCrossing");
            game.Regions.Should().Equal(Region.USA);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task GameCubeParser_ReadsCiso()
    {
        var ciso = new byte[0x8000];
        BinaryPrimitives.WriteUInt32LittleEndian(ciso, 0x4F534943);
        var path = Write("ciso", [.. ciso, .. DiscHeader("GALE01", "Super Smash Bros Melee", wii: false)]);
        try
        {
            (await new GameCubeDiscParser().ProcessFile(path)).Should().ContainSingle()
                .Which.TitleID.Should().Be("GALE01");
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task Parsers_OnlyClaimTheirOwnPlatform()
    {
        var wii = Write("iso", DiscHeader("SMNP01", "New SUPER MARIO BROS. Wii", wii: true));
        try
        {
            (await new GameCubeDiscParser().ProcessFile(wii)).Should().BeEmpty();
            var rom = (await new WiiDiscParser().ProcessFile(wii)).Should().ContainSingle().Subject;
            rom.Regions.Should().Equal(Region.Europe);
        }
        finally { File.Delete(wii); }
    }

    [Fact]
    public void Parse_RejectsNonNintendoData()
    {
        NintendoDiscHeader.Parse(new byte[0x80]).Should().BeNull();
    }
}

using System.Buffers.Binary;
using System.Text;
using FluentAssertions;
using RomManagerShared.Base;
using RomManagerShared.DS.Parsers;
using RomManagerShared.GameBoy.Parsers;
using RomManagerShared.GameBoyAdvance.Parsers;
using RomManagerShared.Nintendo64.Parsers;
using RomManagerShared.SNES.Parsers;

namespace RomManagerTests;

/// <summary>Header parsing for cartridge consoles, using synthetic headers.</summary>
public sealed class CartridgeParserTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;

    public void Dispose() => Directory.Delete(_dir, true);

    private string Write(string name, byte[] data)
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllBytes(path, data);
        return path;
    }

    private static void Ascii(byte[] target, int offset, string text) => Encoding.ASCII.GetBytes(text).CopyTo(target, offset);

    [Fact]
    public async Task GameBoy_DmgCart_UsesFullTitleAndNoGameCode()
    {
        var rom = new byte[0x8000];
        Ascii(rom, 0x134, "KIRBY DREAM LAND");
        rom[0x14A] = 0x01;

        var game = (await new GameBoyRomParser().ProcessFile(Write("kirby.gb", rom))).Should().ContainSingle().Subject;

        game.Titles![0].Value.Should().Be("KIRBY DREAM LAND");
        game.TitleID.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task GameBoy_CgbCart_SplitsTitleAndGameCode()
    {
        var rom = new byte[0x8000];
        Ascii(rom, 0x134, "POKEMON_SLV");
        Ascii(rom, 0x13F, "AAXE");
        rom[0x143] = 0x80;

        var game = (await new GameBoyRomParser().ProcessFile(Write("silver.gbc", rom))).Should().ContainSingle().Subject;

        game.Titles![0].Value.Should().Be("POKEMON_SLV");
        game.TitleID.Should().Be("AAXE");
    }

    [Fact]
    public async Task GameBoyAdvance_ReadsTitleCodeAndRegion()
    {
        var rom = new byte[0x200];
        Ascii(rom, 0xA0, "METROID4USA");
        Ascii(rom, 0xAC, "AMTE");
        Ascii(rom, 0xB0, "01");

        var game = (await new GameBoyAdvanceRomParser().ProcessFile(Write("fusion.gba", rom))).Should().ContainSingle().Subject;

        game.TitleID.Should().Be("AMTE");
        game.Titles![0].Value.Should().Be("METROID4USA");
        game.Regions.Should().Equal(Region.USA);
    }

    [Fact]
    public async Task DS_ReadsGameCodeAndRegion()
    {
        var rom = new byte[0x200];
        Ascii(rom, 0x00, "TETRIS DS");
        Ascii(rom, 0x0C, "ATRP");
        Ascii(rom, 0x10, "01");

        var game = (await new DSNDSParser().ProcessFile(Write("tetris.nds", rom))).Should().ContainSingle().Subject;

        game.TitleID.Should().Be("ATRP");
        game.Titles![0].Value.Should().Be("TETRIS DS");
        game.Regions.Should().Equal(Region.Europe);
    }

    [Fact]
    public async Task DS_GarbageHeader_FallsBackToFileName()
    {
        var rom = new byte[0x200];
        rom[0] = 0xFF;
        Ascii(rom, 0x0C, "g##?");

        var game = (await new DSNDSParser().ProcessFile(Write("Rumble.nds", rom))).Should().ContainSingle().Subject;

        game.TitleID.Should().BeNull();
        game.Titles![0].Value.Should().Be("Rumble");
    }

    private static byte[] N64Header()
    {
        var rom = new byte[0x1000];
        BinaryPrimitives.WriteUInt32BigEndian(rom, 0x80371240);
        Ascii(rom, 0x20, "MARIOKART64".PadRight(20));
        Ascii(rom, 0x3B, "NKTE");
        return rom;
    }

    [Fact]
    public async Task N64_Z64_ReadsHeader()
    {
        var game = (await new Nintendo64RomParser().ProcessFile(Write("mk.z64", N64Header()))).Should().ContainSingle().Subject;

        game.TitleID.Should().Be("KT");
        game.Titles![0].Value.Should().Be("MARIOKART64");
        game.Regions.Should().Equal(Region.USA);
    }

    [Fact]
    public async Task N64_V64_ByteSwapped_ReadsSameHeader()
    {
        var rom = N64Header();
        for (int i = 0; i < rom.Length; i += 2)
            (rom[i], rom[i + 1]) = (rom[i + 1], rom[i]);

        var game = (await new Nintendo64RomParser().ProcessFile(Write("mk.v64", rom))).Should().ContainSingle().Subject;

        game.TitleID.Should().Be("KT");
        game.Titles![0].Value.Should().Be("MARIOKART64");
    }

    [Fact]
    public async Task Snes_LoRom_ReadsTrimmedTitleAndRegion()
    {
        var rom = new byte[0x80000];
        const int header = 0x7FC0;
        Ascii(rom, header, "SUPER MARIOWORLD".PadRight(21));
        rom[header + 0x19] = 0x01; // USA
        BinaryPrimitives.WriteUInt16LittleEndian(rom.AsSpan(header + 0x1C), 0x5F25);
        BinaryPrimitives.WriteUInt16LittleEndian(rom.AsSpan(header + 0x1E), 0xA0DA);

        var game = (await new SNESRomParser().ProcessFile(Write("smw.sfc", rom))).Should().ContainSingle().Subject;

        game.Titles![0].Value.Should().Be("SUPER MARIOWORLD");
        game.Regions.Should().Equal(Region.USA);
    }
}

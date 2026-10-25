using System.IO.Hashing;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using RomManagerShared.Base;
using RomManagerShared.GameGear;
using RomManagerShared.GameGear.Parsers;
using RomManagerShared.Genesis;
using RomManagerShared.Genesis.Parsers;
using RomManagerShared.MasterSystem;
using RomManagerShared.MasterSystem.Parsers;
using RomManagerShared.PCEngine;
using RomManagerShared.PCEngine.Parsers;
using RomManagerShared.Utils.Dat;

namespace RomManagerTests;

public sealed class SegaNecTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;
    public void Dispose() => Directory.Delete(_dir, true);

    private static DatIndex IndexOf(string game, ReadOnlySpan<byte> payload)
    {
        var sha1 = Convert.ToHexStringLower(SHA1.HashData(payload));
        var crc = Crc32.HashToUInt32(payload).ToString("x8");
        var xml = $"""
            <?xml version="1.0"?>
            <datafile>
              <game name="{game}"><rom name="{game}.bin" size="{payload.Length}" crc="{crc}" sha1="{sha1}"/></game>
            </datafile>
            """;
        var index = new DatIndex();
        index.Add(new MemoryStream(Encoding.UTF8.GetBytes(xml)));
        return index;
    }

    private sealed class TestGenesisProvider(DatIndex index) : GenesisDatProvider { protected override DatIndex? GetIndex() => index; }
    private sealed class TestSmsProvider(DatIndex index) : MasterSystemDatProvider { protected override DatIndex? GetIndex() => index; }
    private sealed class TestGgProvider(DatIndex index) : GameGearDatProvider { protected override DatIndex? GetIndex() => index; }
    private sealed class TestPceProvider(DatIndex index) : PCEngineDatProvider { protected override DatIndex? GetIndex() => index; }

    private sealed class Matcher<T>(T provider) where T : class
    {
        public bool Matches(string name) => (bool)typeof(T).GetMethod("MatchesDat", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(provider, [name])!;
    }

    [Theory]
    [InlineData("Sega - Mega Drive - Genesis (20240218-231752).dat", "Genesis")]
    [InlineData("Sega - Master System - Mark III (20240219-202916).dat", "MasterSystem")]
    [InlineData("Sega - Game Gear (20240207-183432).dat", "GameGear")]
    [InlineData("NEC - PC Engine - TurboGrafx-16 (20240101-000000).dat", "PCEngine")]
    [InlineData("NEC - PC Engine SuperGrafx (20240101-000000).dat", "")]
    [InlineData("Sega - 32X (20240101-000000).dat", "")]
    public void DatFileNames_MatchOnlyTheirConsole(string file, string console)
    {
        new Matcher<GenesisDatProvider>(new()).Matches(file).Should().Be(console == "Genesis");
        new Matcher<MasterSystemDatProvider>(new()).Matches(file).Should().Be(console == "MasterSystem");
        new Matcher<GameGearDatProvider>(new()).Matches(file).Should().Be(console == "GameGear");
        new Matcher<PCEngineDatProvider>(new()).Matches(file).Should().Be(console == "PCEngine");
    }

    private string Write(string name, byte[] data)
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllBytes(path, data);
        return path;
    }

    private static void Put(byte[] data, int offset, string text, int width)
    {
        var bytes = Encoding.ASCII.GetBytes(text.PadRight(width));
        bytes.CopyTo(data, offset);
    }

    private static byte[] GenesisRom(string region = "JUE")
    {
        var rom = new byte[2 * 16384];
        for (int i = 0; i < rom.Length; i++) rom[i] = (byte)(i * 13);
        Put(rom, 0x100, "SEGA MEGA DRIVE", 16);
        Put(rom, 0x110, "(C)SEGA 1991.JUN", 16);
        Put(rom, 0x120, "SONIC THE      HEDGEHOG", 48);
        Put(rom, 0x150, "SONIC THE      HEDGEHOG", 48);
        Put(rom, 0x180, "GM 00001009-00", 14);
        Put(rom, 0x1F0, region, 16);
        return rom;
    }

    // Inverse of GenesisFormat.Deinterleave: odd bytes first, even bytes second, per 16 KB block.
    private static byte[] ToSmd(byte[] rom)
    {
        var smd = new byte[512 + rom.Length];
        smd[8] = 0xAA; smd[9] = 0xBB;
        for (int block = 0; block < rom.Length; block += 16384)
            for (int i = 0; i < 8192; i++)
            {
                smd[512 + block + i] = rom[block + i * 2 + 1];
                smd[512 + block + 8192 + i] = rom[block + i * 2];
            }
        return smd;
    }

    [Fact]
    public async Task GenesisParser_ReadsRawHeader()
    {
        var path = Write("sonic.md", GenesisRom());

        var rom = (await new GenesisRomParser().ProcessFile(path)).Should().ContainSingle().Which.Should().BeOfType<GenesisGame>().Subject;

        rom.HeaderFormat.Should().Be("Raw");
        rom.SystemType.Should().Be("SEGA MEGA DRIVE");
        rom.Copyright.Should().Be("(C)SEGA 1991.JUN");
        rom.Titles![0].Value.Should().Be("SONIC THE HEDGEHOG");
        rom.ProductCode.Should().Be("GM 00001009-00");
        rom.Regions.Should().Equal(Region.Japan, Region.USA, Region.Europe);
    }

    [Fact]
    public async Task GenesisParserAndProvider_HandleSmdDumps()
    {
        var raw = GenesisRom();
        var path = Write("sonic.smd", ToSmd(raw));

        var rom = (GenesisGame)(await new GenesisRomParser().ProcessFile(path)).Single();
        rom.HeaderFormat.Should().Be("SMD (interleaved)");
        rom.ProductCode.Should().Be("GM 00001009-00");

        await new TestGenesisProvider(IndexOf("Sonic The Hedgehog (USA, Europe)", raw)).GetTitleInfo(rom);
        rom.Titles![0].Value.Should().Be("Sonic The Hedgehog");
        rom.Regions.Should().Equal(Region.USA, Region.Europe);
    }

    [Fact]
    public async Task GenesisProvider_StripsCopierHeader()
    {
        var raw = GenesisRom();
        var path = Write("sonic.gen", [.. new byte[512], .. raw]);

        var rom = (GenesisGame)(await new GenesisRomParser().ProcessFile(path)).Single();
        rom.HeaderFormat.Should().Be("Copier header");

        await new TestGenesisProvider(IndexOf("Sonic The Hedgehog (Japan)", raw)).GetTitleInfo(rom);
        rom.Titles![0].Value.Should().Be("Sonic The Hedgehog");
    }

    [Theory]
    [InlineData("JUE", new[] { Region.Japan, Region.USA, Region.Europe })]
    [InlineData("U", new[] { Region.USA })]
    [InlineData("E", new[] { Region.Europe })]
    [InlineData("4", new[] { Region.USA })]
    [InlineData("5", new[] { Region.Japan, Region.USA })]
    [InlineData("F", new[] { Region.Japan, Region.Asia, Region.USA, Region.Europe })]
    [InlineData("", new Region[0])]
    public void GenesisRegions_OldAndNewStyle(string code, Region[] expected) =>
        GenesisRomParser.ParseRegions(code).Should().Equal(expected);

    [Fact]
    public async Task GenesisParser_NoHeaderFallsBackToFileName()
    {
        var path = Write("Homebrew Thing.md", new byte[1024]);
        var rom = (await new GenesisRomParser().ProcessFile(path)).Single();
        rom.Titles![0].Value.Should().Be("Homebrew Thing");
    }

    private static byte[] TmrRom(int size, int headerOffset, byte regionNibble)
    {
        var rom = new byte[size];
        for (int i = 0; i < size; i++) rom[i] = (byte)(i * 3);
        "TMR SEGA"u8.CopyTo(rom.AsSpan(headerOffset));
        rom[headerOffset + 0xA] = 0x34; rom[headerOffset + 0xB] = 0x12;
        rom[headerOffset + 0xC] = 0x25; rom[headerOffset + 0xD] = 0x70; // BCD 7025
        rom[headerOffset + 0xE] = 0x12; // leading digit 1, version 2
        rom[headerOffset + 0xF] = (byte)(regionNibble << 4 | 0xC);
        return rom;
    }

    [Fact]
    public void TmrHeader_ParsesProductCodeVersionAndRegion()
    {
        var header = SegaTmrHeader.Parse(TmrRom(0x8000, 0x7FF0, 4))!;
        header.ProductCode.Should().Be("17025");
        header.Version.Should().Be(2);
        header.RegionName.Should().Be("SMS Export");
        header.Checksum.Should().Be(0x1234);

        SegaTmrHeader.Parse(TmrRom(0x4000, 0x3FF0, 3))!.Offset.Should().Be(0x3FF0);
        SegaTmrHeader.Parse(new byte[0x8000]).Should().BeNull();
    }

    [Fact]
    public async Task MasterSystemParserAndProvider_NameHeaderedDump()
    {
        var raw = TmrRom(0x8000, 0x7FF0, 3);
        var path = Write("alex kidd.sms", [.. new byte[512], .. raw]);

        var rom = (await new MasterSystemRomParser().ProcessFile(path)).Should().ContainSingle().Which.Should().BeOfType<MasterSystemGame>().Subject;
        rom.HeaderFormat.Should().Be("TMR SEGA @ 0x7FF0");
        rom.RegionCode.Should().Be("SMS Japan");
        rom.ProductCode.Should().Be("17025");
        rom.Titles![0].Value.Should().Be("alex kidd");

        await new TestSmsProvider(IndexOf("Alex Kidd in Miracle World (USA, Europe)", raw)).GetTitleInfo(rom);
        rom.Titles![0].Value.Should().Be("Alex Kidd in Miracle World");
    }

    [Fact]
    public async Task GameGearParserAndProvider()
    {
        var raw = TmrRom(0x8000, 0x7FF0, 7);
        var path = Write("columns.gg", raw);

        var rom = (await new GameGearRomParser().ProcessFile(path)).Should().ContainSingle().Which.Should().BeOfType<GameGearGame>().Subject;
        rom.RegionCode.Should().Be("GG International");
        rom.Regions.Should().Equal(Region.World);
        rom.Version.Should().Be("2");

        await new TestGgProvider(IndexOf("Columns (USA, Europe)", raw)).GetTitleInfo(rom);
        rom.Titles![0].Value.Should().Be("Columns");
    }

    [Fact]
    public async Task PCEngineParserAndProvider_StripCopierHeader()
    {
        var raw = new byte[3 * 8192];
        for (int i = 0; i < raw.Length; i++) raw[i] = (byte)(i * 5);
        var path = Write("bonk.pce", [.. new byte[512], .. raw]);

        var rom = (await new PCEngineRomParser().ProcessFile(path)).Should().ContainSingle().Which.Should().BeOfType<PCEngineGame>().Subject;
        rom.HeaderFormat.Should().Be("Copier header");
        rom.Titles![0].Value.Should().Be("bonk");

        await new TestPceProvider(IndexOf("Bonk's Adventure (USA)", raw)).GetTitleInfo(rom);
        rom.Titles![0].Value.Should().Be("Bonk's Adventure");
        rom.Regions.Should().Equal(Region.USA);
    }
}

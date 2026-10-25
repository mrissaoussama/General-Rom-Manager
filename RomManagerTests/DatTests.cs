using System.IO.Hashing;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using RomManagerShared.Base;
using RomManagerShared.NES;
using RomManagerShared.NES.Parsers;
using RomManagerShared.Utils.Dat;

namespace RomManagerTests;

public sealed class DatTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;
    public void Dispose() => Directory.Delete(_dir, true);

    private static string Sha1(ReadOnlySpan<byte> d) => Convert.ToHexStringLower(SHA1.HashData(d));
    private static string Crc(ReadOnlySpan<byte> d) => Crc32.HashToUInt32(d).ToString("x8");

    private static DatIndex IndexOf(string game, ReadOnlySpan<byte> payload, string? serial = null)
    {
        var xml = $"""
            <?xml version="1.0"?>
            <!DOCTYPE datafile PUBLIC "-//Logiqx//DTD ROM Management Datafile//EN" "http://www.logiqx.com/dtds/datafile.dtd">
            <datafile>
              <header><name>Test</name></header>
              <game name="{game}">
                <rom name="{game}.bin" size="{payload.Length}" crc="{Crc(payload).ToUpperInvariant()}" sha1="{Sha1(payload).ToUpperInvariant()}" {(serial is null ? "" : $"serial=\"{serial}\"")}/>
              </game>
            </datafile>
            """;
        var index = new DatIndex();
        index.Add(new MemoryStream(Encoding.UTF8.GetBytes(xml)));
        return index;
    }

    private sealed class TestNesProvider(DatIndex index) : NESDatProvider
    {
        protected override DatIndex? GetIndex() => index;
    }

    private sealed class TestN64Provider(DatIndex index) : N64DatProvider
    {
        protected override DatIndex? GetIndex() => index;
    }

    private static byte[] NesFile(byte mapperLow = 1, int prgBanks = 2)
    {
        var data = new byte[16 + prgBanks * 16384];
        "NES\x1A"u8.CopyTo(data);
        data[4] = (byte)prgBanks;
        data[5] = 1;
        data[6] = (byte)(mapperLow << 4);
        for (int i = 16; i < data.Length; i++) data[i] = (byte)(i * 7);
        return data;
    }

    [Fact]
    public void Find_MatchesBySha1OrCrcAndSize()
    {
        byte[] payload = [1, 2, 3, 4];
        var index = IndexOf("Game (USA)", payload);

        index.Find(Sha1(payload), null, 4)!.Game.Should().Be("Game (USA)");
        index.Find(null, Crc(payload), 4).Should().NotBeNull();
        index.Find(null, Crc(payload), 5).Should().BeNull();
    }

    [Theory]
    [InlineData("Super Mario Bros. (World)", "Super Mario Bros.", new[] { Region.World })]
    [InlineData("Zelda II - The Adventure of Link (USA, Europe) (Rev 1)", "Zelda II - The Adventure of Link", new[] { Region.USA, Region.Europe })]
    [InlineData("Some Game (Japan) [b]", "Some Game", new[] { Region.Japan })]
    [InlineData("Other (UK, Hong Kong)", "Other", new[] { Region.UnitedKingdom, Region.HongKong })]
    public void NoIntroNames_SplitIntoTitleAndRegions(string name, string title, Region[] regions)
    {
        DatIndex.CleanTitle(name).Should().Be(title);
        DatIndex.RegionsFromName(name).Should().Equal(regions);
    }

    [Fact]
    public async Task NesParser_ReadsHeader_AndHeaderlessDatNamesIt()
    {
        var data = NesFile(mapperLow: 4, prgBanks: 8);
        var path = Path.Combine(_dir, "smb3.nes");
        File.WriteAllBytes(path, data);

        var rom = (await new NESRomParser().ProcessFile(path)).Should().ContainSingle().Which.Should().BeOfType<NESGame>().Subject;
        rom.Mapper.Should().Be(4);
        rom.PrgRom.Should().Be("128 KB");
        rom.HeaderFormat.Should().Be("iNES");

        var provider = new TestNesProvider(IndexOf("Super Mario Bros. 3 (USA)", data.AsSpan(16), serial: "NES-UM-USA"));
        await provider.GetTitleInfo(rom);

        rom.Titles![0].Value.Should().Be("Super Mario Bros. 3");
        rom.Regions.Should().Equal(Region.USA);
        rom.ProductCode.Should().Be("NES-UM-USA");
        rom.Hashes.Should().Contain(h => h.Type == HashTypeEnum.SHA1 && h.IsVerified);
    }

    [Fact]
    public async Task Provider_LeavesUnknownRomsAlone()
    {
        var path = Path.Combine(_dir, "unknown.nes");
        File.WriteAllBytes(path, NesFile());
        var rom = new NESGame { Path = path };
        rom.AddTitleName("unknown");

        await new TestNesProvider(IndexOf("Other (USA)", [9, 9, 9])).GetTitleInfo(rom);

        rom.Titles!.Should().ContainSingle().Which.Value.Should().Be("unknown");
        rom.Hashes.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task N64Provider_NormalizesByteSwappedDumps()
    {
        byte[] z64 = [0x80, 0x37, 0x12, 0x40, 0xAA, 0xBB, 0xCC, 0xDD];
        byte[] v64 = [0x37, 0x80, 0x40, 0x12, 0xBB, 0xAA, 0xDD, 0xCC];
        var path = Path.Combine(_dir, "game.v64");
        File.WriteAllBytes(path, v64);
        var rom = new Game { Path = path };

        await new TestN64Provider(IndexOf("Game 64 (Europe)", z64)).GetTitleInfo(rom);

        rom.Titles![0].Value.Should().Be("Game 64");
    }
}

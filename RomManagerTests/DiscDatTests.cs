using System.Security.Cryptography;
using AwesomeAssertions;
using RomManagerShared.Base;
using RomManagerShared.Utils.Dat;

namespace RomManagerTests;

public sealed class DiscDatTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("disc").FullName;

    public void Dispose() => Directory.Delete(_dir, true);

    [Fact]
    public async Task HashAsync_MatchesOneShotHashesAcrossBufferBoundaries()
    {
        var data = new byte[(1 << 20) * 2 + 123];
        new Random(42).NextBytes(data);
        var path = Path.Combine(_dir, "game.iso");
        File.WriteAllBytes(path, data);

        var (sha1, crc, size) = await PS2RedumpProvider.HashAsync(path, TestContext.Current.CancellationToken);

        sha1.Should().Be(Convert.ToHexStringLower(SHA1.HashData(data)));
        crc.Should().Be(System.IO.Hashing.Crc32.HashToUInt32(data).ToString("x8"));
        size.Should().Be(data.Length);
    }

    [Fact]
    public async Task Provider_DoesNothingUnlessEnabled()
    {
        var path = Path.Combine(_dir, "game.iso");
        File.WriteAllBytes(path, [1, 2, 3]);
        var rom = new Game { Path = path, TitleID = "SLUS_200.62" };

        PS2RedumpProvider.Enabled.Should().BeFalse();
        (await new PS2RedumpProvider().GetTitleInfo(rom, TestContext.Current.CancellationToken)).Hashes.Should().BeNull();
        new PS2RedumpProvider().DataFingerprint.Should().BeEmpty();
    }

    [Fact]
    public void FirstCueFile_ReadsQuotedAndBareNames()
    {
        DiscDatTitleInfoProvider<RomManagerShared.Interfaces.PS1Console>.FirstCueFile(
            ["REM test", "FILE \"Game (Track 1).bin\" BINARY", "  TRACK 01 MODE2/2352", "FILE \"Game (Track 2).bin\" BINARY"])
            .Should().Be("Game (Track 1).bin");
        DiscDatTitleInfoProvider<RomManagerShared.Interfaces.PS1Console>.FirstCueFile(["FILE game.bin BINARY"]).Should().Be("game.bin");
    }

    [Fact]
    public void FirstGdiFile_ReadsTrackOne()
    {
        DiscDatTitleInfoProvider<RomManagerShared.Interfaces.DreamcastConsole>.FirstGdiFile(
            ["3", "1 0 4 2352 \"Crazy Taxi (Track 1).bin\" 0", "2 756 0 2352 track02.raw 0"]).Should().Be("Crazy Taxi (Track 1).bin");
        DiscDatTitleInfoProvider<RomManagerShared.Interfaces.DreamcastConsole>.FirstGdiFile(["2", "1 0 4 2352 track01.bin 0"]).Should().Be("track01.bin");
    }

    [Fact]
    public void FileToHash_PicksTheImageOrFirstTrack()
    {
        var cue = Path.Combine(_dir, "game.cue");
        File.WriteAllText(cue, "FILE \"game (Track 1).bin\" BINARY");

        PS2RedumpProvider.FileToHash(cue).Should().Be(Path.Combine(_dir, "game (Track 1).bin"));
        PS2RedumpProvider.FileToHash(Path.Combine(_dir, "x.ISO")).Should().EndWith("x.ISO");
        PS2RedumpProvider.FileToHash(Path.Combine(_dir, "x.chd")).Should().BeNull();
    }

    [Fact]
    public async Task HashAsync_StopsWhenCancelled()
    {
        var path = Path.Combine(_dir, "big.iso");
        File.WriteAllBytes(path, new byte[4 << 20]);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => RomManagerShared.Utils.Dat.PS2RedumpProvider.HashAsync(path, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}

using AwesomeAssertions;
using RomManagerShared.Base;
using RomManagerShared.Utils;

namespace RomManagerTests;

public class HashUtilsTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;

    public void Dispose() => Directory.Delete(_dir, true);

    private string Write(string relative, string content)
    {
        var path = Path.Combine(_dir, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public async Task CalculateFileHashes_GivesTheStandardValues()
    {
        var path = Write("abc.bin", "abc");

        var hashes = await HashUtils.CalculateFileHashes(path, [HashTypeEnum.CRC32, HashTypeEnum.MD5, HashTypeEnum.SHA1, HashTypeEnum.SHA256],
            ct: TestContext.Current.CancellationToken);

        hashes.ToDictionary(h => h.Type, h => h.Value).Should().BeEquivalentTo(new Dictionary<HashTypeEnum, string>
        {
            [HashTypeEnum.CRC32] = "352441C2",
            [HashTypeEnum.MD5] = "900150983CD24FB0D6963F7D28E17F72",
            [HashTypeEnum.SHA1] = "A9993E364706816ABA3E25717850C26C9CD0D89D",
            [HashTypeEnum.SHA256] = "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD",
        });
        hashes.Should().AllSatisfy(h => { h.Filename.Should().Be("abc"); h.Extension.Should().Be("bin"); });
    }

    [Fact]
    public async Task CalculateRomHashes_ReplacesEarlierLocalHashesInsteadOfAddingDuplicates()
    {
        var rom = new Game { Path = Write("game.nes", "abc") };

        await HashUtils.CalculateRomHashes(rom, HashUtils.DefaultTypes, ct: TestContext.Current.CancellationToken);
        await HashUtils.CalculateRomHashes(rom, HashUtils.DefaultTypes, ct: TestContext.Current.CancellationToken);

        rom.Hashes.Should().HaveCount(3);
    }

    [Fact]
    public async Task FolderRoms_HashEveryFileIncludingSubfolders()
    {
        Write(Path.Combine("Game", "code", "app.xml"), "<app/>");
        Write(Path.Combine("Game", "content", "deep", "data.bin"), "abc");
        var rom = new RomManagerShared.WiiU.WiiUGame { Path = Path.Combine(_dir, "Game", "code", "app.xml"), IsFolderFormat = true };

        var hashes = await HashUtils.CalculateRomHashes(rom, [HashTypeEnum.CRC32], ct: TestContext.Current.CancellationToken);

        hashes.Select(h => h.Filename).Should().BeEquivalentTo(["app", "data"]);
    }

    [Fact]
    public async Task HashRomsAsync_SkipsMissingFilesAndReportsFullProgress()
    {
        var present = new Game { Path = Write("a.nes", "abc") };
        var missing = new Game { Path = Path.Combine(_dir, "gone.nes") };
        var saved = new List<RomHash>();
        var reports = new List<double>();

        var count = await HashUtils.HashRomsAsync([present, missing], h => { saved.AddRange(h); return Task.CompletedTask; },
            new InlineProgress(reports.Add), TestContext.Current.CancellationToken);

        count.Should().Be(1);
        saved.Should().HaveCount(HashUtils.DefaultTypes.Count);
        reports.Should().NotBeEmpty().And.BeInAscendingOrder();
        reports[^1].Should().Be(100);
    }

    private sealed class InlineProgress(Action<double> report) : IProgress<double>
    {
        public void Report(double value) => report(value);
    }
}

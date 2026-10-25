using FluentAssertions;
using RomManagerShared.Base;
using RomManagerShared.Utils;

namespace RomManagerTests;

public class DuplicateDetectorTests
{
    // ── Helpers ───────────────────────────────────────────────────────────

    private static Game MakeGame(string titleId, string title = "Game",
        string? version = null, Region region = Region.Unknown, string? path = null,
        string? hashValue = null)
    {
        var rom = new Game
        {
            TitleID = titleId,
            Titles = [new RomTitle(title)],
            Version = version,
            Regions = region == Region.Unknown ? [] : [region],
            Path = path ?? $"C:/roms/{titleId}.nsp"
        };

        if (hashValue is not null)
            rom.Hashes = [new RomHash { Type = HashTypeEnum.CRC32, Value = hashValue }];

        return rom;
    }

    // ── TitleID strategy ──────────────────────────────────────────────────

    [Fact]
    public void FindDuplicates_ByTitleId_FindsGroup()
    {
        var detector = new DuplicateDetector(DuplicateStrategy.TitleId);
        var roms = new List<Rom>
        {
            MakeGame("0100AAAA", version: "1.0"),
            MakeGame("0100AAAA", version: "2.0"),
            MakeGame("0100BBBB")   // different title — not a duplicate
        };

        var groups = detector.FindDuplicates(roms);

        groups.Should().HaveCount(1);
        groups[0].Roms.Should().HaveCount(2);
        groups[0].MatchedBy.Should().Be(DuplicateStrategy.TitleId);
    }

    [Fact]
    public void FindDuplicates_ByTitleId_NoDuplicatesReturnsEmpty()
    {
        var detector = new DuplicateDetector(DuplicateStrategy.TitleId);
        var roms = new List<Rom>
        {
            MakeGame("0100AAAA"),
            MakeGame("0100BBBB")
        };

        detector.FindDuplicates(roms).Should().BeEmpty();
    }

    // ── Hash strategy ─────────────────────────────────────────────────────

    [Fact]
    public void FindDuplicates_ByHash_FindsGroup()
    {
        var detector = new DuplicateDetector(DuplicateStrategy.Hash);
        const string sameHash = "DEADBEEF";
        var roms = new List<Rom>
        {
            MakeGame("0100AA", hashValue: sameHash),
            MakeGame("0100BB", hashValue: sameHash),
            MakeGame("0100CC", hashValue: "00000000")  // different hash
        };

        var groups = detector.FindDuplicates(roms);
        groups.Should().HaveCount(1);
        groups[0].Roms.Should().HaveCount(2);
    }

    // ── Ranking ───────────────────────────────────────────────────────────

    [Fact]
    public void DuplicateGroup_Preferred_IsHighestVersion()
    {
        var detector = new DuplicateDetector(DuplicateStrategy.TitleId);
        var roms = new List<Rom>
        {
            MakeGame("0100AAAA", version: "1.0"),
            MakeGame("0100AAAA", version: "3.0"),
            MakeGame("0100AAAA", version: "2.0"),
        };

        var groups = detector.FindDuplicates(roms);
        groups.Should().HaveCount(1);

        var preferred = groups[0].Preferred;
        ((Game)preferred).Version.Should().Be("3.0");
    }

    [Fact]
    public void DuplicateGroup_Redundant_ExcludesPreferred()
    {
        var detector = new DuplicateDetector(DuplicateStrategy.TitleId);
        var roms = new List<Rom>
        {
            MakeGame("0100AAAA", version: "1.0"),
            MakeGame("0100AAAA", version: "2.0"),
        };

        var groups = detector.FindDuplicates(roms);
        groups[0].Redundant.Should().HaveCount(1);
        ((Game)groups[0].Redundant[0]).Version.Should().Be("1.0");
    }

    // ── Title name strategy ───────────────────────────────────────────────

    [Fact]
    public void FindDuplicates_ByTitleName_NormalisesArticles()
    {
        var detector = new DuplicateDetector(DuplicateStrategy.TitleName);
        var roms = new List<Rom>
        {
            MakeGame("0100AA", title: "The Legend"),
            MakeGame("0100BB", title: "legend"),
        };

        var groups = detector.FindDuplicates(roms);
        groups.Should().HaveCount(1);
    }

    // ── FindRedundantRoms ─────────────────────────────────────────────────

    [Fact]
    public void FindRedundantRoms_ReturnsAllButBest()
    {
        var detector = new DuplicateDetector(DuplicateStrategy.TitleId);
        var r1 = MakeGame("0100AAAA", version: "1.0");
        var r2 = MakeGame("0100AAAA", version: "2.0");

        var redundant = detector.FindRedundantRoms([r1, r2]);
        redundant.Should().HaveCount(1);
        ((Game)redundant[0]).Version.Should().Be("1.0");
    }
}

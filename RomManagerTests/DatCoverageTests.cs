using System.Text;
using AwesomeAssertions;
using RomManagerShared.Base;
using RomManagerShared.Utils.Dat;

namespace RomManagerTests;

public class DatCoverageTests
{
    private static DatIndex Index(params string[] games)
    {
        var xml = "<datafile>" + string.Concat(games.Select((g, i) =>
            $"<game name=\"{g}\"><rom name=\"{g}.nes\" size=\"16\" crc=\"0000000{i}\" sha1=\"{i:x40}\"/></game>")) + "</datafile>";
        var index = new DatIndex();
        index.Add(new MemoryStream(Encoding.UTF8.GetBytes(xml)));
        return index;
    }

    private static Rom Verified(string game) => new Game
    {
        Hashes = [new RomHash { Type = HashTypeEnum.SHA1, Value = "x", IsVerified = true, Description = game }],
    };

    [Fact]
    public void Compute_SplitsHaveAndMissingByVerifiedHash()
    {
        var index = Index("Zelda (USA)", "Mario (USA)", "Metroid (Europe)");
        var unverified = new Game { Hashes = [new RomHash { Type = HashTypeEnum.SHA1, Value = "y", Description = "Metroid (Europe)" }] };

        var coverage = DatCoverage.Compute(index, [Verified("Zelda (USA)"), Verified("Zelda (USA)"), unverified, new Game()]);

        coverage.Total.Should().Be(3);
        coverage.Have.Should().Equal("Zelda (USA)");
        coverage.Missing.Should().Equal("Mario (USA)", "Metroid (Europe)");
        index.Games.Should().HaveCount(3);
    }

    [Fact]
    public void Compute_MergesSeveralDatSources()
    {
        var coverage = DatCoverage.Compute([Index("A"), Index("B")], [Verified("B")]);

        coverage!.Total.Should().Be(2);
        coverage.Have.Should().Equal("B");
        coverage.Missing.Should().Equal("A");
        DatCoverage.Compute([], []).Should().BeNull();
    }

    [Fact]
    public void Verification_SplitsVerifiedFromUnmatchedGames()
    {
        var good = new Game { Path = "/b.gb", Hashes = [new RomHash { Type = HashTypeEnum.SHA1, Value = "1", IsVerified = true }] };
        var hack = new Game { Path = "/a.gb", Hashes = [new RomHash { Type = HashTypeEnum.SHA1, Value = "2" }] };
        var update = new Update { Path = "/u.gb" };

        var result = RomManagerShared.Utils.Dat.DatVerification.Compute([good, hack, update]);

        result.Verified.Should().Equal(good);
        result.Unmatched.Should().Equal(hack);
    }
}

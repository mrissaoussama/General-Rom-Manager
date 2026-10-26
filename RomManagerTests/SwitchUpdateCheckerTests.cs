using System.Text;
using AwesomeAssertions;
using RomManagerShared.Base;
using RomManagerShared.Switch;

namespace RomManagerTests;

public class SwitchUpdateCheckerTests
{
    private static Dictionary<string, uint> Parse(string json)
        => SwitchUpdateChecker.ParseLatestVersions(new MemoryStream(Encoding.UTF8.GetBytes(json)));

    private static SwitchGame Game(string id, string title, string? version = "0")
    {
        var game = new SwitchGame { TitleID = id, Version = version };
        game.AddTitleName(title);
        return game;
    }

    [Fact]
    public void ParseLatestVersions_ComparesNumerically()
    {
        // "65536" > "131072" as strings; the newest is 131072.
        var latest = Parse("""
            {
              "0100abcd12340000": { "65536": "2018-01-01", "131072": "2019-01-01", "196608x": "bad" },
              "0100000000010000": {},
              "not-an-object": 5
            }
            """);

        latest.Should().ContainSingle();
        latest["0100ABCD12340000"].Should().Be(131072);
    }

    [Fact]
    public void FindMissing_UsesNewestInstalledUpdateAndSkipsUpToDateGames()
    {
        var latest = Parse("""
            {
              "0100000000010000": { "65536": "", "131072": "" },
              "0100000000020000": { "65536": "" },
              "0100000000030000": { "65536": "" },
              "0100000000040000": { "65536": "" }
            }
            """);
        List<Rom> library =
        [
            Game("0100000000010000", "Behind"),
            new SwitchUpdate { TitleID = "0100000000010800", Version = "65536" },
            Game("0100000000020000", "Current"),
            new SwitchUpdate { TitleID = "0100000000020800", Version = "65536" },
            Game("0100000000030000", "No update", version: null),
            new SwitchUpdate { TitleID = "0100000000040800", Version = "0" }, // update without its game
            new SwitchDLC { TitleID = "0100000000011001", Version = "999999" },
        ];

        var missing = SwitchUpdateChecker.FindMissing(library, latest);

        missing.Select(m => (m.Title, m.InstalledVersion, m.LatestVersion)).Should().Equal(
            ("Behind", "65536", "131072"),
            ("No update", "0", "65536"));
    }

    [Fact]
    public void FindMissing_IgnoresGamesUnknownToTheDatabase()
    {
        var missing = SwitchUpdateChecker.FindMissing([Game("0100000000090000", "Homebrew")], Parse("{}"));

        missing.Should().BeEmpty();
    }
}

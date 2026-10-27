using System.Text.Json;
using AwesomeAssertions;
using RomManagerShared.Switch;

namespace RomManagerTests;

public class SwitchDlcCheckerTests
{
    [Theory]
    [InlineData("0100A0D004FB1001", "0100A0D004FB0000")]
    [InlineData("01007EF00011F001", "01007EF00011E000")]
    [InlineData("0100A0D004FB0000", null)] // base game
    [InlineData("0100A0D004FB0800", null)] // update
    [InlineData("nope", null)]
    public void BaseOfDlc(string dlc, string? expected) => SwitchDlcChecker.BaseOfDlc(dlc).Should().Be(expected);

    [Fact]
    public void FindMissing_ListsCatalogDlcOfOwnedGamesOnly()
    {
        var game = new SwitchGame { TitleID = "0100A0D004FB0000" };
        game.AddTitleName("Game");
        var ownedDlc = new SwitchDLC { TitleID = "0100A0D004FB1001" };
        var titledb = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("""
            {
              "70010000000001": { "id": "0100A0D004FB1001", "name": "Owned pack" },
              "70010000000002": { "id": "0100A0D004FB1002", "name": "Missing pack" },
              "0100A0D004FB0000": { "id": "0100A0D004FB0000", "name": "Game" },
              "70010000000003": { "id": "0100BBBBBBBB1001", "name": "DLC of a game we do not have" }
            }
            """)!;

        var missing = SwitchDlcChecker.FindMissing([game, ownedDlc], SwitchDlcChecker.Catalog(titledb));

        var m = missing.Should().ContainSingle().Subject;
        m.TitleId.Should().Be("0100A0D004FB1002");
        m.Name.Should().Be("Missing pack");
        m.GameTitle.Should().Be("Game");
    }
}

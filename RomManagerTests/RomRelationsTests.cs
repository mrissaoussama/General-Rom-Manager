using AwesomeAssertions;
using RomManagerShared.Base;
using RomManagerShared.Utils;

namespace RomManagerTests;

public class RomRelationsTests
{
    [Theory]
    [InlineData("0004000000030700", "0004000000030700")] // 3DS game
    [InlineData("0004000E00030700", "0004000000030700")] // 3DS update
    [InlineData("0004008C00030700", "0004000000030700")] // 3DS DLC
    [InlineData("0005000E10101D00", "0005000010101D00")] // Wii U update
    [InlineData("01007EF00011E000", "01007EF00011E000")] // Switch game
    [InlineData("01007EF00011E800", "01007EF00011E000")] // Switch update
    [InlineData("01007EF00011F001", "01007EF00011E000")] // Switch DLC
    [InlineData("ules00590", "ULES00590")]               // PSP: updates reuse the ID
    [InlineData("", null)]
    [InlineData(null, null)]
    public void BaseTitleId(string? titleId, string? expected) =>
        RomRelations.BaseTitleId(titleId).Should().Be(expected);

    [Fact]
    public void Link_AttachesUpdatesAndDlcToTheirGameAndSharesItsIcon()
    {
        var game = new Game { TitleID = "0004000000030700", Icon = "icon.png" };
        var update = new Update { TitleID = "0004000E00030700" };
        var dlc = new DLC { TitleID = "0004008C00030700", Icon = "own.png" };
        var orphan = new Update { TitleID = "0004000E00099900" };

        RomRelations.Link([update, game, dlc, orphan]);

        game.Updates.Should().Equal(update);
        game.DLCs.Should().Equal(dlc);
        update.RelatedGame.Should().BeSameAs(game);
        dlc.RelatedGame.Should().BeSameAs(game);
        orphan.RelatedGame.Should().BeNull();
        update.Icon.Should().Be("icon.png");
        dlc.Icon.Should().Be("own.png");
    }

    [Fact]
    public void Link_AgainReplacesEarlierLinks()
    {
        var game = new Game { TitleID = "ULES00590" };
        var update = new Update { TitleID = "ULES00590" };
        RomRelations.Link([game, update]);

        RomRelations.Link([game, update]);

        game.Updates.Should().ContainSingle();
    }

    [Fact]
    public void LinksAreNotStoredInTheLibraryCache()
    {
        var game = new Game { TitleID = "ULES00590" };
        var update = new Update { TitleID = "ULES00590" };
        RomRelations.Link([game, update]);

        var json = RomManagerShared.Base.Library.RomSnapshot.Serialize([game, update]);

        json.Should().NotContain("RelatedGame").And.NotContain("Updates");
    }

    [Fact]
    public void SizeOnDisk_CountsEachFileOnce()
    {
        Rom[] roms =
        [
            new Game { Path = "a.nsp", Size = 100 },
            new Update { Path = "a.nsp", Size = 100 }, // same file
            new Game { Path = "b.3ds", Size = 50 },
            new Game { Path = "c.3ds" },
        ];

        FileUtils.SizeOnDisk(roms).Should().Be(150);
    }

    [Theory]
    [InlineData("CTR-P-AMKE", "https://art.gametdb.com/3ds/cover/US/AMKE.jpg")]
    [InlineData("CTR-P-AMKP", "https://art.gametdb.com/3ds/cover/EN/AMKP.jpg")]
    [InlineData("CTR-P-AMKJ", "https://art.gametdb.com/3ds/cover/JA/AMKJ.jpg")]
    [InlineData("", null)]
    [InlineData("CTR-P-A-K", null)]
    public void ThreeDSCoverUrl(string code, string? expected) =>
        RomManagerShared.ThreeDS.ThreeDSUtils.GameTdbCoverUrl(code).Should().Be(expected);
}

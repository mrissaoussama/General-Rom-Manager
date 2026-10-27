using AwesomeAssertions;
using RomManagerShared.Base;
using RomManagerShared.WiiU;

namespace RomManagerTests;

public class RomMergeTests
{
    [Fact]
    public void MergeFrom_PrefersSetValues_AndKeepsFlags()
    {
        var target = new Game { Id = 7, TitleID = "A", Publisher = "Old", IsFolderFormat = true, NumberOfPlayers = 2, Size = 10 };
        var other = new Game { TitleID = "A", Publisher = "New", Developer = " ", IsFolderFormat = false, Size = null };

        target.MergeFrom(other);

        target.Id.Should().Be(7);
        target.Publisher.Should().Be("New");
        target.Developer.Should().BeNull(); // whitespace does not overwrite
        target.IsFolderFormat.Should().BeTrue(); // an unset false never clears a flag
        target.NumberOfPlayers.Should().Be(2);
        target.Size.Should().Be(10);
    }

    [Fact]
    public void MergeFrom_CombinesListsWithoutDuplicates()
    {
        var target = new Game();
        target.AddTitleName("Zelda");
        target.AddRegion(Region.USA);
        var other = new Game();
        other.AddTitleName("Zelda");
        other.AddTitleName("ゼルダ");
        other.AddRegion(Region.USA);
        other.AddRegion(Region.Japan);

        target.MergeFrom(other);

        target.Titles!.Select(t => t.Value).Should().Equal("Zelda", "ゼルダ");
        target.Regions.Should().Equal(Region.USA, Region.Japan);
    }

    [Fact]
    public void MergeFrom_CopiesSubclassPropertiesIntoAMoreSpecificType()
    {
        var parsed = new WiiUUpdate { TitleID = "0005000E101C9500", RelatedGameTitleID = "00050000101C9500", Path = "/x/code/app.xml" };
        var typed = new WiiUUpdate();

        typed.MergeFrom(parsed);

        typed.RelatedGameTitleID.Should().Be("00050000101C9500");
        typed.Path.Should().Be("/x/code/app.xml");

        var game = new WiiUGame();
        game.MergeFrom(parsed); // WiiUGame has no RelatedGameTitleID: skipped, base fields still copied
        game.TitleID.Should().Be("0005000E101C9500");
    }
}

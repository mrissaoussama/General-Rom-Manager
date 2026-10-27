using AwesomeAssertions;
using RomManagerShared.Base;

namespace RomManagerTests;

public class PluginHooksTests
{
    [Fact]
    public void Diff_FindsAddedAndRemovedRoms()
    {
        var kept = new Game { Path = "/roms/a.nsp", TitleID = "A" };
        var gone = new Game { Path = "/roms/b.nsp", TitleID = "B" };
        var updateOfKept = new Update { Path = "/roms/a.nsp", TitleID = "A" };
        var keptAgain = new Game { Path = "/roms/a.nsp", TitleID = "A" };
        var fresh = new Game { Path = "/roms/c.nsp", TitleID = "C" };

        var scan = LibraryScanCompleted.Diff("Switch", [kept, gone], [keptAgain, updateOfKept, fresh]);

        scan.Console.Should().Be("Switch");
        scan.Roms.Should().HaveCount(3);
        scan.Added.Should().BeEquivalentTo(new Rom[] { updateOfKept, fresh }, o => o.WithStrictOrdering().ExcludingMissingMembers());
        scan.Removed.Should().ContainSingle().Which.Should().BeSameAs(gone);
    }

    [Fact]
    public void Diff_FirstScanAddsEverything()
    {
        var a = new Game { Path = "/roms/a.nsp", TitleID = "A" };

        var scan = LibraryScanCompleted.Diff("Switch", [], [a]);

        scan.Added.Should().ContainSingle();
        scan.Removed.Should().BeEmpty();
    }
}

using AwesomeAssertions;
using RomManagerShared.Base;
using RomManagerShared.Interfaces;

namespace RomManagerTests;

public class ConsoleDetectorTests
{
    private static readonly Dictionary<Type, IReadOnlyCollection<string>> Extensions = new()
    {
        [typeof(SwitchConsole)] = ["nsp", "nsz", "xci"],
        [typeof(PS2Console)] = ["iso", "cso", "chd"],
        [typeof(PSPConsole)] = ["iso", "cso", "pbp"],
        [typeof(PS1Console)] = ["cue", "chd"],
    };

    [Fact]
    public void Rank_UniqueExtensionsWin()
    {
        var guesses = ConsoleDetector.Rank(["a.nsp", "b.nsz", "c.iso", "notes.txt"], Extensions);

        guesses[0].Console.Should().Be(typeof(SwitchConsole));
        guesses[0].Files.Should().Be(2);
    }

    [Fact]
    public void Rank_SharedExtensionsSplitTheirWeight()
    {
        var guesses = ConsoleDetector.Rank(["a.iso", "b.iso", "c.pbp"], Extensions);

        guesses[0].Console.Should().Be(typeof(PSPConsole));
        guesses.Should().Contain(g => g.Console == typeof(PS2Console) && g.Score == 1.0);
    }

    [Fact]
    public void Rank_NoKnownFiles_ReturnsEmpty() =>
        ConsoleDetector.Rank(["readme.md"], Extensions).Should().BeEmpty();
}

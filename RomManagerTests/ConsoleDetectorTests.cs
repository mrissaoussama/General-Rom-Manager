using AwesomeAssertions;
using RomManagerShared.Base;
using RomManagerShared.Interfaces;

using RomManagerShared.PS1;
using RomManagerShared.PS2;
using RomManagerShared.PSP;
using RomManagerShared.Switch;

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

    [Fact]
    public void Rank_SharedExtensionsGoToTheConsoleWhoseParserAcceptsTheHeader()
    {
        var dir = Directory.CreateTempSubdirectory("detect").FullName;
        try
        {
            var files = Enumerable.Range(0, 3).Select(i => Path.Combine(dir, $"{i}.iso")).ToList();
            foreach (var f in files) File.WriteAllBytes(f, [0x50]); // 'P' for PSP

            var guesses = ConsoleDetector.Rank(files, Extensions,
                (console, _, header) => console == typeof(PSPConsole) ? header[0] == 0x50 : header[0] == 0x32);

            guesses.Should().ContainSingle().Which.Console.Should().Be(typeof(PSPConsole));
        }
        finally { Directory.Delete(dir, true); }
    }
}

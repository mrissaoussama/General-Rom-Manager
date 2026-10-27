using System.Globalization;
using AwesomeAssertions;
using RomManagerShared;
using RomManagerShared.Base;
using RomManagerShared.Switch;
using RomManagerShared.Utils;

namespace RomManagerTests;

/// <summary>Results that must not depend on the user's locale, and file names that must work everywhere.</summary>
public class CultureAndNamingTests
{
    private sealed class FakeParser(List<string> extensions) : IRomParser<SwitchConsole>
    {
        public List<string> Extensions { get; set; } = extensions;
        public Task<List<Rom>> ProcessFile(string path) => Task.FromResult<List<Rom>>([new Game { TitleID = "0100" }]);
    }

    private static T WithCulture<T>(string culture, Func<T> action)
    {
        var saved = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo(culture);
        try { return action(); }
        finally { CultureInfo.CurrentCulture = saved; }
    }

    [Theory]
    [InlineData("ar-SA")] // Hijri calendar
    [InlineData("th-TH")] // Buddhist calendar
    [InlineData("fr-FR")]
    public void ReleaseDateAndYear_AreGregorianInEveryLocale(string culture)
    {
        var rom = new Game { Path = "game.nsp", TitleID = "0100", Titles = [new RomTitle("Game")], ReleaseDate = new DateOnly(2020, 5, 1) };

        var name = WithCulture(culture, () => FileRenamer.GenerateFileName([rom], "{TitleName} {ReleaseDate} ({Year})"));

        name.Should().Be("Game 2020-05-01 (2020).nsp");
    }

    [Fact]
    public async Task ExtensionsMatchWithTurkishCasing()
    {
        var executor = new RomParserExecutor<SwitchConsole>([new FakeParser(["iso"])]);

        // In tr-TR "ISO".ToLower() is "ıso" (dotless i), which matched no parser.
        var saved = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
        try
        {
            var roms = await executor.ExecuteParsers("GAME.ISO", TestContext.Current.CancellationToken);
            roms.Should().ContainSingle();
        }
        finally { CultureInfo.CurrentCulture = saved; }
    }

    [Fact]
    public void Rename_RefusesAnEmptyName()
    {
        var dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var path = Path.Combine(dir, "game.nsp");
            File.WriteAllText(path, "x");
            var rom = new Game { Path = path, TitleID = "0100" }; // no title

            var preview = FileRenamer.PreviewRename([rom], "{TitleName}").Should().ContainSingle().Subject;
            preview.Success.Should().BeFalse();
            preview.ErrorMessage.Should().Be(FileRenamer.EmptyNameError);

            FileRenamer.RenameFiles([rom], "{TitleName}");
            File.Exists(path).Should().BeTrue();
            File.Exists(Path.Combine(dir, ".nsp")).Should().BeFalse();
        }
        finally { Directory.Delete(dir, true); }
    }

    [Theory]
    [InlineData("Zelda: Breath of the Wild?.nsp", "Zelda_ Breath of the Wild_.nsp")]
    [InlineData("A \"quoted\" <name>.iso", "A _quoted_ _name_.iso")]
    [InlineData("Ends with dots... .nsp", "Ends with dots.nsp")]
    [InlineData("CON.nsp", "CON_.nsp")]
    [InlineData("nul", "nul_")]
    [InlineData("Normal Name (USA).sfc", "Normal Name (USA).sfc")]
    public void SanitizeFileName_GivesNamesThatWorkOnWindowsAndFat(string input, string expected) =>
        FileRenamer.SanitizeFileName(input).Should().Be(expected);

    [Fact]
    public void SanitizeFileName_CapsLengthAndKeepsTheExtension()
    {
        var name = FileRenamer.SanitizeFileName(new string('a', 300) + ".nsp");

        name.Length.Should().Be(255);
        name.Should().EndWith("a.nsp");
    }

    [Fact]
    public void Languages_Placeholder_ListsKnownLanguagesAndDropsItsBracketsWhenEmpty()
    {
        var withLanguages = new Game
        {
            Path = "g.nsp", Titles = [new RomTitle("Game")],
            Languages = [Language.English, Language.French, Language.Unknown, Language.English],
        };
        var without = new Game { Path = "g.nsp", Titles = [new RomTitle("Game")] };

        FileRenamer.GenerateFileName([withLanguages], "{TitleName} ({Languages})").Should().Be("Game (English,French).nsp");
        FileRenamer.GenerateFileName([without], "{TitleName} ({Languages})").Should().Be("Game.nsp");
    }
}

public class VersionKeyTests
{
    [Theory]
    [InlineData("1.10", "1.9")]
    [InlineData("2.0", "1.10")]
    [InlineData("v1.2.3", "1.2")]
    [InlineData("65536", "0")]
    [InlineData("01.02", "01.01")]
    [InlineData("1.0.1", "")]
    public void Orders_NumericallyByPart(string newer, string older)
    {
        VersionKey.Compare(newer, older).Should().BePositive();
        VersionKey.Compare(older, newer).Should().BeNegative();
    }

    [Theory]
    [InlineData("1.2", "1.2.0")]
    [InlineData("01.02", "1.2")]
    [InlineData(null, "")]
    [InlineData("abc", "0")]
    public void Equal_WhenOnlyFormattingDiffers(string? a, string? b) =>
        VersionKey.Parse(a).Should().Be(VersionKey.Parse(b));

    [Fact]
    public void DuplicateDetector_KeepsTheHighestVersion_InAnyLocale()
    {
        var saved = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR"); // "1.9" did not parse as a number here
        try
        {
            var older = new Update { TitleID = "0100", Version = "1.9", Path = "a.nsp" };
            var newer = new Update { TitleID = "0100", Version = "1.10", Path = "b.nsp" };

            var group = new DuplicateDetector(DuplicateStrategy.TitleId).FindDuplicates([older, newer]).Should().ContainSingle().Subject;

            group.Preferred.Should().BeSameAs(newer);
        }
        finally { CultureInfo.CurrentCulture = saved; }
    }
}

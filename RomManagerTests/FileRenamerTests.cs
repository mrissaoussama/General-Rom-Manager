using AwesomeAssertions;
using RomManagerShared.Base;
using RomManagerShared.Utils;

namespace RomManagerTests;

public class FileRenamerTests
{
    // ── Helper ────────────────────────────────────────────────────────────

    private static Rom MakeRom(string path, string titleId, string title,
        string? version = null, Region region = Region.Unknown, string? publisher = null)
        => new Game
        {
            Path = path,
            TitleID = titleId,
            Titles = [new RomTitle(title)],
            Version = version,
            Regions = region == Region.Unknown ? [] : [region],
            Publisher = publisher
        };

    // ── GenerateFileName tests ────────────────────────────────────────────

    [Fact]
    public void PreviewRename_ReportsNewNamesWithoutTouchingFiles()
    {
        var dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var path = Path.Combine(dir, "old.nsp");
            File.WriteAllText(path, "x");
            var rom = MakeRom(path, "0100", "New Name");

            var result = FileRenamer.PreviewRename([rom], "{TitleName}").Should().ContainSingle().Subject;

            Path.GetFileName(result.DestinationPath).Should().Be("New Name.nsp");
            File.Exists(path).Should().BeTrue();
            rom.Path.Should().Be(path);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void GenerateFileName_ReplacesTitleName()
    {
        var rom = MakeRom("C:/roms/game.nsp", "01001234", "Super Game");
        // Extension is always appended from the source path; do not include it in format
        string result = FileRenamer.GenerateFileName([rom], "{TitleName}");
        result.Should().StartWith("Super Game");
        result.Should().EndWith(".nsp"); // extension appended from source path
    }

    [Fact]
    public void GenerateFileName_ReplacesTitleID()
    {
        var rom = MakeRom("C:/roms/game.nsp", "01001234ABCD", "Game");
        string result = FileRenamer.GenerateFileName([rom], "[{TitleID}]{TitleName}");
        result.Should().Contain("01001234ABCD");
    }

    [Fact]
    public void GenerateFileName_StripsEmptyBracketRegion()
    {
        var rom = MakeRom("C:/roms/game.nsp", "0100", "Game", region: Region.Unknown);
        string result = FileRenamer.GenerateFileName([rom], "{TitleName} [{Region}]");
        result.Should().NotContain("[{Region}]");
        result.Should().NotContain("[]");
    }

    [Fact]
    public void GenerateFileName_IncludesRegionWhenPresent()
    {
        var rom = MakeRom("C:/roms/game.nsp", "0100", "Game", region: Region.USA);
        string result = FileRenamer.GenerateFileName([rom], "{TitleName} [{Region}]");
        result.Should().Contain("USA");
    }

    [Fact]
    public void GenerateFileName_StripsVersionBracketWhenEmpty()
    {
        var rom = MakeRom("C:/roms/game.nsp", "0100", "Game", version: null);
        string result = FileRenamer.GenerateFileName([rom], "{TitleName} [v{Version}]");
        result.Should().NotContain("[v{Version}]");
        result.Should().NotContain("v]");
    }

    [Fact]
    public void GenerateFileName_IncludesVersionWhenPresent()
    {
        var rom = MakeRom("C:/roms/game.nsp", "0100", "Game", version: "1.0.0");
        string result = FileRenamer.GenerateFileName([rom], "{TitleName} [v{Version}]");
        result.Should().Contain("1.0.0");
    }

    [Fact]
    public void GenerateFileName_IncludesPublisher()
    {
        var rom = MakeRom("C:/roms/game.nsp", "0100", "Game", publisher: "Nintendo");
        string result = FileRenamer.GenerateFileName([rom], "{TitleName} ({Publisher})");
        result.Should().Contain("Nintendo");
    }

    [Fact]
    public void GenerateFileName_CollapseDoubleSpaces()
    {
        // When region is empty: "Game  .nsp" → "Game .nsp" → trimmed
        var rom = MakeRom("C:/roms/game.nsp", "0100", "Game", region: Region.Unknown);
        string result = FileRenamer.GenerateFileName([rom], "{TitleName} {Region}.nsp");
        result.Should().NotContain("  ");
    }

    // ── PreviewRenames tests ──────────────────────────────────────────────

    [Fact]
    public void PreviewRenames_DoesNotModifyRomPath()
    {
        var rom = MakeRom("C:/roms/old_name.nsp", "0100", "New Name");
        var before = rom.Path;

        FileRenamer.PreviewRenames([rom], "{TitleName}");

        rom.Path.Should().Be(before, "Preview should not touch the file or path");
    }

    [Fact]
    public void PreviewRenames_ReturnsWasDryRunTrue()
    {
        var rom = MakeRom("C:/roms/old_name.nsp", "0100", "New Name");
        var results = FileRenamer.PreviewRenames([rom], "{TitleName}");
        results.Should().AllSatisfy(r => r.WasDryRun.Should().BeTrue());
    }

    // ── Bug-fix: sourcePath must not re-combine directory + full path ─────

    [Fact]
    public void GenerateFileName_NullRomListIsHandledGracefully()
    {
        var act = () => FileRenamer.RenameFiles(null!, "{TitleName}");
        act.Should().NotThrow();
    }

    [Fact]
    public void RenameFiles_EmptyListReturnsEmptyResults()
    {
        var results = FileRenamer.RenameFiles([], "{TitleName}");
        results.Should().BeEmpty();
    }

    // ── RomGroup with multiple files: version picked from Update ─────────

    [Fact]
    public void GenerateFileName_PicksVersionFromUpdateInGroup()
    {
        var game    = MakeRom("C:/roms/g.nsp",   "0100", "Title");
        var update  = new Update { Path = "C:/roms/u.nsp", TitleID = "0100", Version = "2.0", Titles = [new RomTitle("Title")] };

        string result = FileRenamer.GenerateFileName([game, update], "{TitleName} [v{Version}]");
        result.Should().Contain("2.0");
    }

    [Fact]
    public async Task RenameFiles_RenamesTheFolderOfFolderFormatRoms()
    {
        var dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var game = Path.Combine(dir, "dump0001");
            Directory.CreateDirectory(Path.Combine(game, "code"));
            File.WriteAllText(Path.Combine(game, "code", "app.xml"), "<app/>");
            var rom = new RomManagerShared.WiiU.WiiUGame { Path = Path.Combine(game, "code", "app.xml"), IsFolderFormat = true };
            rom.AddTitleName("Zelda");

            var result = (await FileRenamer.RenameFilesAsync([rom], "{TitleName}", ct: TestContext.Current.CancellationToken))
                .Should().ContainSingle().Subject;

            result.Success.Should().BeTrue();
            File.Exists(Path.Combine(dir, "Zelda", "code", "app.xml")).Should().BeTrue();
            rom.Path.Should().Be(Path.Combine(dir, "Zelda", "code", "app.xml"));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public async Task Rename_ThatOnlyChangesCase_IsApplied()
    {
        var dir = Directory.CreateTempSubdirectory("case").FullName;
        try
        {
            var path = Path.Combine(dir, "zelda.nes");
            File.WriteAllText(path, "x");
            var rom = new RomManagerShared.Base.Game { Path = path, TitleID = "1" };
            rom.AddTitleName("Zelda");

            var result = (await FileRenamer.RenameFilesAsync([rom], "{TitleName}", ct: TestContext.Current.CancellationToken)).Single();

            result.Success.Should().BeTrue();
            result.Changed.Should().BeTrue();
            Directory.GetFiles(dir).Select(Path.GetFileName).Should().Equal("Zelda.nes");
        }
        finally { Directory.Delete(dir, true); }
    }

    // ── Placeholder spellings and console properties ──────────────────────

    private sealed class CartRom : Game
    {
        [UiProperty(DisplayName = "Save Type")]
        public string? SaveType { get; set; }
        public int Mapper { get; set; }
    }

    [Fact]
    public void GenerateFileName_AcceptsColumnNamesWithSpacesAndAnyCase()
    {
        var rom = MakeRom("C:/roms/game.cia", "0004000000030700", "Mario Kart 7");
        rom.ProductCode = "CTR-P-AMKE";
        rom.MinimumFirmware = "11.0";

        FileRenamer.GenerateFileName([rom], "{Product Code} {title name} [{Min Firmware}]")
            .Should().Be("CTR-P-AMKE Mario Kart 7 [11.0].cia");
    }

    [Fact]
    public void GenerateFileName_FileNameIsTheCurrentNameAndBracesInItAreKept()
    {
        var rom = MakeRom("C:/roms/{TitleName} old.cia", "1", "New");

        FileRenamer.GenerateFileName([rom], "{FileName} - {TitleName}").Should().Be("{TitleName} old - New.cia");
    }

    [Fact]
    public void GenerateFileName_FillsConsolePropertiesByNameOrDisplayName()
    {
        var rom = new CartRom { Path = "C:/roms/a.nes", TitleID = "1", Titles = [new RomTitle("Zelda")], SaveType = "SRAM", Mapper = 4 };

        FileRenamer.GenerateFileName([rom], "{TitleName} [{Save Type}] (m{Mapper})").Should().Be("Zelda [SRAM] (m4).nes");
        FileRenamer.UnknownPlaceholders(rom, "{SaveType} {Mapper}").Should().BeEmpty();
    }

    [Fact]
    public void GenerateFileName_DropsEmptyBracketsOfConsoleProperties()
    {
        var rom = new CartRom { Path = "C:/roms/a.nes", TitleID = "1", Titles = [new RomTitle("Zelda")] };

        FileRenamer.GenerateFileName([rom], "{TitleName} [{SaveType}]").Should().Be("Zelda.nes");
    }

    [Fact]
    public void Rename_WithAnUnknownPlaceholder_FailsInsteadOfWritingItIntoTheName()
    {
        var rom = MakeRom("C:/roms/game.cia", "1", "Game");

        FileRenamer.UnknownPlaceholders(rom, "{TitleName} {Bogus}").Should().Equal("{Bogus}");
        var result = FileRenamer.PreviewRename([rom], "{TitleName} {Bogus}").Single();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("{Bogus}");
    }

    [Fact]
    public void ConsolePlaceholders_ListsVisibleUiPropertiesOfTheRomTypes()
    {
        FileRenamer.ConsolePlaceholders([typeof(CartRom)]).Should().Equal("{SaveType}");
    }

    [Fact]
    public void GenerateFileName_EmptyVersionAndDlcCountLeaveNoPlaceholder()
    {
        var rom = new Update { Path = "C:/roms/u.cia", TitleID = "1", Titles = [new RomTitle("Game")] };

        FileRenamer.GenerateFileName([rom], "{TitleName} {Version} {DLCCount}").Should().Be("Game.cia");
    }
}

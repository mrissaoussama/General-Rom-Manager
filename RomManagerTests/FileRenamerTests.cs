using FluentAssertions;
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
}

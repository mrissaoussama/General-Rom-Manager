using AwesomeAssertions;
using RomManagerShared.Base;
using RomManagerShared.Utils;

namespace RomManagerTests;

public class RomMoverTests : IDisposable
{
    // Use temporary directories so no real files are needed for dry-run tests
    private readonly string _tempSource;
    private readonly string _tempTarget;

    public RomMoverTests()
    {
        _tempSource = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _tempTarget = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempSource);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempSource))
            Directory.Delete(_tempSource, recursive: true);
        if (Directory.Exists(_tempTarget))
            Directory.Delete(_tempTarget, recursive: true);
    }

    // ── Folder roms and shared files ──────────────────────────────────────

    [Fact]
    public void MoveFiles_MovesWholeFolderForFolderFormatRoms()
    {
        var game = Path.Combine(_tempSource, "Zelda [00050000101C9500]");
        Directory.CreateDirectory(Path.Combine(game, "code"));
        Directory.CreateDirectory(Path.Combine(game, "content", "empty"));
        File.WriteAllText(Path.Combine(game, "code", "app.xml"), "<app/>");
        File.WriteAllText(Path.Combine(game, "content", "data.bin"), "data");
        var rom = new RomManagerShared.WiiU.WiiUGame { Path = Path.Combine(game, "code", "app.xml"), IsFolderFormat = true };

        var preview = new RomMover(_tempTarget).PreviewMoves([rom]).Should().ContainSingle().Which;
        preview.SourcePath.Should().Be(game);

        var result = new RomMover(_tempTarget).MoveFiles([rom]).Should().ContainSingle().Which;

        result.Success.Should().BeTrue();
        Directory.Exists(game).Should().BeFalse();
        var moved = Path.Combine(_tempTarget, "Zelda [00050000101C9500]");
        File.ReadAllText(Path.Combine(moved, "content", "data.bin")).Should().Be("data");
        Directory.Exists(Path.Combine(moved, "content", "empty")).Should().BeTrue();
        rom.Path.Should().Be(Path.Combine(moved, "code", "app.xml"));
    }

    [Fact]
    public void MoveFiles_MovesASharedFileOnceAndUpdatesEveryRom()
    {
        var file = Path.Combine(_tempSource, "bundle.nsp");
        File.WriteAllText(file, "x");
        var game = new Game { Path = file, TitleID = "0100000000010000" };
        var update = new Update { Path = file, TitleID = "0100000000010800" };

        var results = new RomMover(_tempTarget).MoveFiles([game, update]);

        results.Should().ContainSingle().Which.Success.Should().BeTrue();
        game.Path.Should().Be(Path.Combine(_tempTarget, "bundle.nsp"));
        update.Path.Should().Be(game.Path);
    }

    [Fact]
    public void RestorePaths_UndoesFolderAndSharedFileMoves()
    {
        var game = Path.Combine(_tempSource, "Game");
        Directory.CreateDirectory(Path.Combine(game, "code"));
        File.WriteAllText(Path.Combine(game, "code", "app.xml"), "<app/>");
        File.WriteAllText(Path.Combine(game, "data.bin"), "data");
        var folderRom = new RomManagerShared.WiiU.WiiUGame { Path = Path.Combine(game, "code", "app.xml"), IsFolderFormat = true };
        var file = Path.Combine(_tempSource, "bundle.nsp");
        File.WriteAllText(file, "x");
        var a = new Game { Path = file };
        var b = new Update { Path = file };
        List<(Rom, string?)> original = [(folderRom, folderRom.Path), (a, file), (b, file)];

        new RomMover(_tempTarget).MoveFiles([folderRom, a, b]);
        RomOperationService.RestorePaths(original, TestContext.Current.CancellationToken);

        File.ReadAllText(Path.Combine(game, "data.bin")).Should().Be("data");
        folderRom.Path.Should().Be(Path.Combine(game, "code", "app.xml"));
        File.Exists(file).Should().BeTrue();
        a.Path.Should().Be(file);
        b.Path.Should().Be(file);
        Directory.EnumerateFileSystemEntries(_tempTarget).Should().BeEmpty();
    }

    [Fact]
    public void MoveDirectoryVerified_RenamesOnTheSameVolume()
    {
        var src = Path.Combine(_tempSource, "dir");
        Directory.CreateDirectory(src);
        File.WriteAllText(Path.Combine(src, "a.txt"), "a");
        var dest = Path.Combine(_tempSource, "moved");

        RomMover.MoveDirectoryVerified(src, dest, TestContext.Current.CancellationToken);

        Directory.Exists(src).Should().BeFalse();
        File.ReadAllText(Path.Combine(dest, "a.txt")).Should().Be("a");
    }

    // ── Dry-run ───────────────────────────────────────────────────────────

    [Fact]
    public void PreviewMoves_ReturnsWasDryRunTrue()
    {
        var mover = new RomMover(_tempTarget);
        var rom = new Game { Path = Path.Combine(_tempSource, "game.nsp") };

        var results = mover.PreviewMoves([rom]);
        results.Should().AllSatisfy(r => r.WasDryRun.Should().BeTrue());
    }

    [Fact]
    public void PreviewMoves_DoesNotCreateTargetDirectory()
    {
        var mover = new RomMover(_tempTarget);
        var rom = new Game { Path = Path.Combine(_tempSource, "game.nsp") };

        mover.PreviewMoves([rom]);

        Directory.Exists(_tempTarget).Should().BeFalse("Dry-run must not create directories");
    }

    [Fact]
    public void PreviewMoves_DoesNotModifyRomPath()
    {
        var mover = new RomMover(_tempTarget);
        var rom = new Game { Path = Path.Combine(_tempSource, "game.nsp") };
        var original = rom.Path;

        mover.PreviewMoves([rom]);

        rom.Path.Should().Be(original);
    }

    // ── Real move ─────────────────────────────────────────────────────────

    [Fact]
    public void MoveFiles_MovesFileToTargetDirectory()
    {
        // Create a real source file
        var srcFile = Path.Combine(_tempSource, "real_game.nsp");
        File.WriteAllText(srcFile, "dummy");

        var rom = new Game { Path = srcFile };
        var mover = new RomMover(_tempTarget);

        var results = mover.MoveFiles([rom]);

        results.Should().HaveCount(1);
        results[0].Success.Should().BeTrue();
        File.Exists(Path.Combine(_tempTarget, "real_game.nsp")).Should().BeTrue();
        File.Exists(srcFile).Should().BeFalse("Source should have been moved");
    }

    [Fact]
    public void MoveFiles_UpdatesRomPath()
    {
        var srcFile = Path.Combine(_tempSource, "game2.nsp");
        File.WriteAllText(srcFile, "data");
        var rom = new Game { Path = srcFile };
        var mover = new RomMover(_tempTarget);

        mover.MoveFiles([rom]);

        rom.Path.Should().StartWith(_tempTarget);
    }

    [Fact]
    public void MoveFiles_HandlesEmptyList()
    {
        var mover = new RomMover(_tempTarget);
        var results = mover.MoveFiles([]);
        results.Should().BeEmpty();
    }
}

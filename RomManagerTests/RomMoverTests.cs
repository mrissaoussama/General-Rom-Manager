using FluentAssertions;
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

using AwesomeAssertions;
using RomManagerShared.Utils;

namespace RomManagerTests;

public sealed class TrashTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("trash").FullName;

    public void Dispose() => Directory.Delete(_dir, true);

    [Fact]
    public void Freedesktop_MovesFileAndWritesTrashInfo()
    {
        var file = Path.Combine(_dir, "my game.nsp");
        File.WriteAllText(file, "x");
        var dataHome = Path.Combine(_dir, "share");

        var first = Trash.TrashFreedesktop(file, dataHome);
        File.WriteAllText(file, "y");
        var second = Trash.TrashFreedesktop(file, dataHome);

        File.Exists(file).Should().BeFalse();
        Path.GetFileName(first).Should().Be("my game.nsp");
        Path.GetFileName(second).Should().Be("my game 2.nsp");
        var info = File.ReadAllText(Path.Combine(dataHome, "Trash", "info", "my game.nsp.trashinfo"));
        info.Should().StartWith("[Trash Info]").And.Contain("my%20game.nsp").And.Contain("DeletionDate=");
    }

    [Fact]
    public void MoveToTrash_MissingFileThrows() =>
        FluentActions.Invoking(() => Trash.MoveToTrash(Path.Combine(_dir, "nope"))).Should().Throw<FileNotFoundException>();

    // Explicit: leaves a small file in the real Recycle Bin. Run with --explicit on.
    [Fact(Explicit = true)]
    public void MoveToTrash_RecyclesOnWindowsFixedDrives()
    {
        if (!OperatingSystem.IsWindows() || new DriveInfo(Path.GetPathRoot(_dir)!).DriveType != DriveType.Fixed)
            return;
        var file = Path.Combine(_dir, $"recycle-test-{Guid.NewGuid():N}.tmp");
        File.WriteAllText(file, "x");

        Trash.MoveToTrash(file);

        File.Exists(file).Should().BeFalse();
    }
}

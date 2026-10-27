using AwesomeAssertions;
using RomManagerShared.Base.Library;

namespace RomManagerTests;

public class ScanOptionsTests
{
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "roms");
    private static string F(params string[] parts) => Path.Combine([Root, .. parts]);

    [Fact]
    public void PlainNames_MatchAFolderAtAnyDepth()
    {
        var options = ScanOptions.For(Root, null, ["Backups"]);

        options.IsExcluded(Root, F("Backups", "a.nsp")).Should().BeTrue();
        options.IsExcluded(Root, F("Games", "backups", "a.nsp")).Should().BeTrue();
        options.IsExcluded(Root, F("BackupsOld", "a.nsp")).Should().BeFalse();
        options.IsExcluded(Root, F("Backups.nsp")).Should().BeFalse();
    }

    [Fact]
    public void Paths_AreRelativeToTheRoot()
    {
        var options = ScanOptions.For(Root, null, [@"Updates\old"]);

        options.IsExcluded(Root, F("Updates", "old", "a.nsp")).Should().BeTrue();
        options.IsExcluded(Root, F("Updates", "old", "deep", "a.nsp")).Should().BeTrue();
        options.IsExcluded(Root, F("Updates", "a.nsp")).Should().BeFalse();
        options.IsExcluded(Root, F("Other", "Updates", "old", "a.nsp")).Should().BeFalse();
    }

    [Fact]
    public void Parallelism_IsClampedAndDefaultsByDrive()
    {
        ScanOptions.For(Root, 0, null).MaxParallelism.Should().Be(1);
        ScanOptions.For(Root, 100, null).MaxParallelism.Should().Be(32);
        ScanOptions.For(Root, null, null).MaxParallelism.Should().Be(ScanOptions.DefaultParallelism);
        ScanOptions.For(Root, null, ["", " / "]).ExcludedFolders.Should().BeEmpty();
    }
}

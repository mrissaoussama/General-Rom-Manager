using AvaloniaUI.Utils;
using AwesomeAssertions;

namespace RomManagerTests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("settings").FullName;
    private string FilePath => Path.Combine(_dir, "settings.json");

    public void Dispose() => Directory.Delete(_dir, true);

    [Fact]
    public void SaveAndReload_RoundTrip()
    {
        var store = new JsonSettingsStore(FilePath);
        store.Current.ConsoleLibraryPaths["NES"] = "/roms/nes";
        store.Save(store.Current);

        new JsonSettingsStore(FilePath).Current.ConsoleLibraryPaths["NES"].Should().Be("/roms/nes");
        File.Exists(FilePath + ".tmp").Should().BeFalse();
    }

    [Fact]
    public void CorruptFile_IsKeptAsideNotOverwritten()
    {
        File.WriteAllText(FilePath, "{ \"ConsoleLibraryPaths\": { \"NES\": ");
        var store = new JsonSettingsStore(FilePath);

        store.Current.ConsoleLibraryPaths.Should().BeEmpty();
        var backup = Directory.EnumerateFiles(_dir, "settings.corrupt-*.json").Should().ContainSingle().Subject;
        File.ReadAllText(backup).Should().Contain("NES");
    }

    [Fact]
    public void MissingFile_GivesDefaults() =>
        new JsonSettingsStore(FilePath).Current.RenameFormatString.Should().NotBeNullOrEmpty();
}

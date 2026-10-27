using AvaloniaUI.Utils;
using AvaloniaUI.ViewModels;
using AwesomeAssertions;

namespace RomManagerTests;

public class SettingsViewModelTests
{
    private static (SettingsViewModel Vm, Settings Settings, List<Settings> Saved) Create(Settings? settings = null)
    {
        settings ??= new Settings();
        var saved = new List<Settings>();
        return (new SettingsViewModel(settings, persist: saved.Add), settings, saved);
    }

    [Fact]
    public void Cancel_LeavesSettingsUntouched()
    {
        var (vm, settings, saved) = Create();
        var nes = vm.Consoles.First(c => c.Id == "NES");

        nes.Primary.Value = "/roms/nes";
        vm.RenameFormat = "{TitleID}";
        vm.CancelCommand.Execute(null);
        vm.OnClosed();

        settings.ConsoleLibraryPaths.Should().BeEmpty();
        settings.RenameFormatString.Should().NotBe("{TitleID}");
        saved.Should().BeEmpty();
    }

    [Fact]
    public void Save_WritesTheDraft()
    {
        var (vm, settings, saved) = Create();
        var nes = vm.Consoles.First(c => c.Id == "NES");
        nes.Primary.Value = " /roms/nes ";
        nes.ExtraFolders = "/nas/nes; ; /usb/nes";
        nes.EmulatorPath = "/bin/emu";
        nes.Parallelism = "2";
        nes.ExcludedFolders = "Backups";
        vm.Tools[0].Path = "/bin/chdman";
        vm.RegionPriority = "Europe, USA";

        vm.ConfirmCommand.Execute(null);
        vm.OnClosed();

        saved.Should().ContainSingle();
        settings.ConsoleLibraryPaths["NES"].Should().Be("/roms/nes");
        settings.ConsoleExtraLibraryPaths["NES"].Should().Equal("/nas/nes", "/usb/nes");
        settings.Emulators["NES"].Path.Should().Be("/bin/emu");
        settings.ScanSettings["NES"].Parallelism.Should().Be(2);
        settings.ScanSettings["NES"].ExcludedFolders.Should().Equal("Backups");
        settings.ToolPaths.Should().ContainKey(vm.Tools[0].Tool.Id);
        settings.RegionPriority.Should().Be("Europe, USA");
    }

    [Fact]
    public void Save_ClearsWhatWasEmptied()
    {
        var settings = new Settings();
        settings.ConsoleLibraryPaths["NES"] = "/old";
        settings.Emulators["NES"] = new EmulatorSettings { Path = "/emu" };
        var (vm, _, _) = Create(settings);
        var nes = vm.Consoles.First(c => c.Id == "NES");
        nes.Primary.Value.Should().Be("/old");

        nes.Primary.Value = "";
        nes.EmulatorPath = " ";
        vm.Apply();

        settings.ConsoleLibraryPaths.Should().NotContainKey("NES");
        settings.Emulators.Should().NotContainKey("NES");
    }

    [Fact]
    public void InsertPlaceholder_GoesAtTheCaret()
    {
        var (vm, _, _) = Create(new Settings { RenameFormatString = "AB" });
        vm.RenameCaret = 1;

        vm.InsertPlaceholderCommand.Execute("{X}");

        vm.RenameFormat.Should().Be("A{X}B");
        vm.RenameCaret.Should().Be(4);
    }

    [Fact]
    public async Task PathField_ReportsWhetherTheFolderExists()
    {
        var field = new PathField(Path.GetTempPath());
        await field.Checked;
        field.Status.Text.Should().Be("Found");

        field.Value = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        await field.Checked;
        field.Status.Text.Should().Be("Not found");

        field.Value = "";
        await field.Checked;
        field.Status.Text.Should().Be("Not set");
    }
}

using AvaloniaUI.Utils;
using AvaloniaUI.ViewModels;
using AwesomeAssertions;
using RomManagerShared.Base;

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

    [Theory]
    [InlineData("12", 12)]
    [InlineData("", 0)]
    [InlineData("-3", 0)]
    [InlineData("soon", 0)]
    public void RescanInterval_IsSavedAsHours(string text, int hours)
    {
        var (vm, settings, _) = Create();
        vm.RescanIntervalHours = text;

        vm.Apply();

        settings.RescanIntervalHours.Should().Be(hours);
    }

    private sealed class FakeOptions : RomManagerShared.Interfaces.IConsoleOptions
    {
        public IReadOnlyDictionary<string, string>? Applied { get; private set; }
        public string Title => "Fake";
        public IReadOnlyList<PluginSettingDescriptor> Options =>
            [new("Url", "URL", Default: "https://default"), new("Online", "Online", PluginSettingKind.Boolean, "false")];
        public void Apply(IReadOnlyDictionary<string, string> values) => Applied = values;
    }

    [Fact]
    public void ConsoleOptions_AreEditedSavedAndApplied()
    {
        var options = new FakeOptions();
        var settings = new Settings();
        var vm = new SettingsViewModel(settings, persist: _ => { }, consoleOptions: [options]);
        var group = vm.ConsoleGroups.Should().ContainSingle().Subject;
        group.Fields[0].Value.Should().Be("https://default");

        group.Fields[0].Value = "https://mine";
        group.Fields[1].BoolValue = true;
        vm.Apply();

        settings.ConsoleOptions["FakeOptions"].Should().Equal(new Dictionary<string, string> { ["Url"] = "https://mine", ["Online"] = "true" });
        options.Applied!["Online"].Should().Be("true");
    }

    [Fact]
    public void LegacySwitchAndPs4Settings_MoveIntoConsoleOptions()
    {
        var settings = new Settings { SwitchTitleDBUrlOverride = "https://mirror/", AllowPs4OnlineUpdateCheck = true };

        ConsoleOptionsStore.MigrateLegacy(settings).Should().BeTrue();

        settings.ConsoleOptions["SwitchOptions"]["TitleDbUrl"].Should().Be("https://mirror/");
        settings.ConsoleOptions["PS4Options"]["OnlineUpdateCheck"].Should().Be("true");
        settings.SwitchTitleDBUrlOverride.Should().BeNull();
        ConsoleOptionsStore.MigrateLegacy(settings).Should().BeFalse();
    }
}

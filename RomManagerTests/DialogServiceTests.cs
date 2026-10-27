using AvaloniaUI.Utils;
using AvaloniaUI.ViewModels;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using RomManagerShared.Base;
using RomManagerShared.Base.Library;
using RomManagerShared.Interfaces;

namespace RomManagerTests;

/// <summary>View models ask <see cref="IDialogService"/>; these tests answer for the user.</summary>
[Collection(nameof(DialogServiceTests))] // swaps the static DialogService.Current
public sealed class DialogServiceTests : IDisposable
{
    private sealed class FakeDialogs : IDialogService
    {
        public bool Answer { get; set; }
        public string? Folder { get; set; }
        public List<string> Asked { get; } = [];

        public Task<bool> ConfirmAsync(string title, string message, string? confirmText = null)
        {
            Asked.Add(title);
            return Task.FromResult(Answer);
        }

        public Task<bool> PreviewAsync(string title, string summary, IEnumerable<string> lines, string confirmText, bool showCancel = true)
        {
            Asked.Add(title);
            return Task.FromResult(Answer);
        }

        public Task ShowPropertiesAsync(RomViewModel rom) => Task.CompletedTask;
        public Task<ConsoleDescriptor?> ChooseConsoleAsync(string folder, IReadOnlyList<ConsoleDescriptor> consoles, ConsoleDescriptor? guess, string detail) =>
            Task.FromResult(guess);
        public Task<string?> AskRenameTemplateAsync(IReadOnlyList<Rom> roms, IEnumerable<string> consoleProperties, string consoleName, string template) =>
            Task.FromResult<string?>(null);
        public Task<string?> PickFolderAsync(string title) => Task.FromResult(Folder);
        public Task<string?> PickFileAsync(string title, string filterName, params string[] extensions) => Task.FromResult(Folder);
        public Task<bool> ShowAsync(DialogViewModel dialog) => Task.FromResult(Answer);
        public Task<string?> PickSaveFileAsync(string title, string suggestedName, params (string Name, string Extension)[] types) =>
            Task.FromResult<string?>(null);
    }

    private sealed class TestConsoleViewModel() : ConsoleViewModelBase("Test", null)
    {
        public void Show(params Rom[] roms) => ShowRoms(roms);
        protected override Task LoadAsync(bool fullRescan) => Task.CompletedTask;
        public override IReadOnlyList<(string PropName, string Header, bool UsesDictionaryLookup)> GetColumnDescriptors() => [];
        public override IEnumerable<IConsoleRomAction> GetContextActionsForSelection(IEnumerable<Rom> selectedRoms) => [];
        public override IReadOnlyList<ConsoleRequirement> GetAllRequirements() => [];
    }

    private readonly IDialogService _previous = DialogService.Current;
    private readonly FakeDialogs _dialogs = new();
    private readonly string _dir = Directory.CreateTempSubdirectory("dialogs").FullName;

    public DialogServiceTests() => DialogService.Current = _dialogs;

    public void Dispose()
    {
        DialogService.Current = _previous;
        Directory.Delete(_dir, true);
    }

    private (TestConsoleViewModel Vm, string File) OneRom()
    {
        var file = Path.Combine(_dir, "game.nes");
        File.WriteAllText(file, "x");
        var vm = new TestConsoleViewModel();
        var rom = new Game { Path = file, TitleID = "1" };
        rom.AddTitleName("Game");
        vm.Show(rom);
        return (vm, file);
    }

    [Fact]
    public async Task Delete_DoesNothingWhenTheUserSaysNo()
    {
        var (vm, file) = OneRom();
        _dialogs.Answer = false;

        await vm.DeleteCommand.ExecuteAsync([.. vm.Roms]);

        _dialogs.Asked.Should().Equal("Delete");
        File.Exists(file).Should().BeTrue();
        vm.Roms.Should().ContainSingle();
    }

    [Fact]
    public async Task Move_StopsAtThePreviewWhenNotConfirmed()
    {
        var (vm, file) = OneRom();
        vm.Operations = new RomOperationService(new OperationQueue(), new ServiceCollection().BuildServiceProvider());
        _dialogs.Folder = Path.Combine(_dir, "target");
        _dialogs.Answer = false;

        await vm.MoveCommand.ExecuteAsync([.. vm.Roms]);

        _dialogs.Asked.Should().Equal("Move");
        File.Exists(file).Should().BeTrue();
        vm.Operations.Queue.Operations.Should().BeEmpty();
    }

    [Fact]
    public void RenameDialog_PreviewsAndInsertsPlaceholders()
    {
        var rom = new Game { Path = Path.Combine(_dir, "old.nes"), TitleID = "1" };
        rom.AddTitleName("New");
        var vm = new RenameDialogViewModel([rom], ["Mapper"], "NES", "{TitleName}");

        vm.PreviewRows.Should().ContainSingle().Which.Should().Be("old.nes  →  New.nes");
        vm.HasConsolePlaceholders.Should().BeTrue();
        vm.ConsolePlaceholders.Should().Equal("{Mapper}");

        vm.CaretIndex = vm.Template.Length;
        vm.InsertCommand.Execute(" [{TitleID}]");
        vm.Template.Should().Be("{TitleName} [{TitleID}]");
        vm.PreviewRows.Should().ContainSingle().Which.Should().Be("old.nes  →  New [1].nes");
        vm.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ChooseConsoleDialog_NeedsASelection()
    {
        var a = new ConsoleDescriptor(typeof(object), "A", "Console A");
        var vm = new ChooseConsoleDialogViewModel("folder", [a], null, "");
        vm.ConfirmCommand.CanExecute(null).Should().BeFalse();

        vm.Selected = a;
        vm.ConfirmCommand.CanExecute(null).Should().BeTrue();
        vm.ConfirmCommand.Execute(null);
        vm.Confirmed.Should().BeTrue();
    }

    [Fact]
    public async Task OneGameOneRom_MovesTheOtherVersionsAfterThePreview()
    {
        var vm = new TestConsoleViewModel();
        Game Version(string name, Region region)
        {
            var file = Path.Combine(_dir, name + ".gb");
            File.WriteAllText(file, name);
            var rom = new Game { Path = file, TitleID = name };
            rom.AddTitleName("Tetris");
            rom.AddRegion(region);
            return rom;
        }
        var usa = Version("Tetris (USA)", Region.USA);
        var jp = Version("Tetris (Japan)", Region.Japan);
        vm.Show(usa, jp);
        var queue = new OperationQueue();
        vm.Operations = new RomOperationService(queue, new ServiceCollection().BuildServiceProvider());
        _dialogs.Answer = true;
        _dialogs.Folder = Path.Combine(_dir, "others");

        await vm.OneGameOneRomCommand.ExecuteAsync(null);

        _dialogs.Asked.Should().Equal("1G1R");
        var op = queue.Operations.Should().ContainSingle().Subject;
        for (int i = 0; i < 200 && !op.IsFinished; i++) await Task.Delay(20, TestContext.Current.CancellationToken);
        File.Exists(Path.Combine(_dir, "others", "Tetris (Japan).gb")).Should().BeTrue();
        File.Exists(usa.Path).Should().BeTrue();
    }

    private sealed class FakeUpdater(string? version, bool installed = true) : IAppUpdater
    {
        public bool IsInstalled => installed;
        public bool Applied { get; private set; }
        public Task<string?> CheckAsync(CancellationToken ct = default) => Task.FromResult(version);
        public Task DownloadAndRestartAsync(IProgress<int>? progress = null) { Applied = true; return Task.CompletedTask; }
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task AppUpdate_AsksBeforeInstalling(bool answer, bool applied)
    {
        var updater = new FakeUpdater("2.0.0");
        var vm = new MainViewModel(new ServiceCollection().BuildServiceProvider()) { Updater = updater };
        _dialogs.Answer = answer;

        await vm.CheckForAppUpdateAsync(quiet: true);

        _dialogs.Asked.Should().Equal("Update available");
        updater.Applied.Should().Be(applied);
    }

    [Fact]
    public async Task AppUpdate_NothingToDoWhenUpToDateOrNotInstalled()
    {
        var vm = new MainViewModel(new ServiceCollection().BuildServiceProvider()) { Updater = new FakeUpdater(null) };
        await vm.CheckForAppUpdateAsync(quiet: false);
        vm.StatusMessage.Should().Be("RomManager is up to date");

        vm.Updater = new FakeUpdater("2.0.0", installed: false);
        await vm.CheckForAppUpdateAsync(quiet: false);
        vm.StatusMessage.Should().Contain("installed copies only");
        _dialogs.Asked.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveOfflineFolders_DropsThemFromSettings()
    {
        var previous = Settings.Store;
        Settings.Store = new JsonSettingsStore(Path.Combine(_dir, "settings.json"));
        try
        {
            Settings.Instance.ConsoleLibraryPaths["Test"] = "/gone/primary";
            Settings.Instance.ConsoleExtraLibraryPaths["Test"] = ["/nas/still-here", "/gone/extra"];
            var vm = new TestConsoleViewModel { OfflineFolders = ["/gone/primary", "/gone/extra"] };
            vm.OfflineText.Should().Contain("2 folders not reachable");
            _dialogs.Answer = true;

            await vm.RemoveOfflineFoldersCommand.ExecuteAsync(null);

            Settings.Instance.ConsoleLibraryPaths.Should().NotContainKey("Test");
            Settings.Instance.ConsoleExtraLibraryPaths["Test"].Should().Equal("/nas/still-here");
            vm.HasOfflineFolders.Should().BeFalse();
            new JsonSettingsStore(Path.Combine(_dir, "settings.json")).Current.ConsoleLibraryPaths.Should().NotContainKey("Test");
        }
        finally { Settings.Store = previous; }
    }
}

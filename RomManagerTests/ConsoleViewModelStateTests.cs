using AvaloniaUI.ViewModels;
using AwesomeAssertions;
using RomManagerShared.Base;
using RomManagerShared.Base.Library;
using RomManagerShared.Interfaces;

namespace RomManagerTests;

public class ConsoleViewModelStateTests
{
    private sealed class TestConsoleViewModel(string name = "Test") : ConsoleViewModelBase(name, null)
    {
        public void Show(params Rom[] roms) => ShowRoms(roms);
        public void SetUnrecognized(params string[] paths) => Unrecognized = [.. paths.Select(p => new UnrecognizedFile(p, "test"))];
        protected override Task LoadAsync(bool fullRescan) => Task.CompletedTask;
        public override IReadOnlyList<(string PropName, string Header, bool UsesDictionaryLookup)> GetColumnDescriptors() => [];
        public override IEnumerable<IConsoleRomAction> GetContextActionsForSelection(IEnumerable<Rom> selectedRoms) => [];
        public override IReadOnlyList<ConsoleRequirement> GetAllRequirements() => [];
    }

    private static T Named<T>(T rom, string id, string title, long size = 0) where T : Rom
    {
        rom.TitleID = id;
        rom.Path = Path.Combine(Path.GetTempPath(), "roms", $"{title}.nsp");
        rom.Size = size;
        rom.AddTitleName(title);
        return rom;
    }

    [Fact]
    public void EmptyState_ShowsOnlyWhenNotLoadingAndEmpty()
    {
        var vm = new TestConsoleViewModel();
        vm.ShowEmptyState.Should().BeTrue();

        vm.IsLoading = true;
        vm.ShowEmptyState.Should().BeFalse();

        vm.IsLoading = false;
        vm.Show(Named(new Game(), "0100000000010000", "Game"));
        vm.ShowEmptyState.Should().BeFalse();
        vm.RomCount.Should().Be(1);
    }

    [Fact]
    public void UnrecognizedText_CountsFiles()
    {
        var vm = new TestConsoleViewModel();
        vm.UnrecognizedText.Should().BeNull();

        vm.SetUnrecognized("a.nsp", "b.nsp");

        vm.UnrecognizedText.Should().Be("2 file(s) not recognised");
    }

    [Fact]
    public void Grouping_PutsUpdatesAndDlcUnderTheirGame()
    {
        var vm = new TestConsoleViewModel();
        vm.Show(
            Named(new Game(), "0100000000010000", "Zelda"),
            Named(new Update(), "0100000000010800", "Zelda update"),
            Named(new DLC(), "0100000000011001", "Zelda DLC"),
            Named(new Game(), "0100000000020000", "Mario"));

        vm.Roms.Where(r => r.TitleID.StartsWith("0100000000010")).Select(r => r.GroupKey).Should().AllBe("Zelda");
        vm.Roms.Single(r => r.Title == "Mario").GroupKey.Should().Be("Mario");

        vm.IsGrouped = true;
        vm.RomsView.GroupDescriptions.Should().ContainSingle();
    }

    [Fact]
    public void Dashboard_TotalsAcrossConsoles()
    {
        var switchVm = new TestConsoleViewModel("Switch");
        switchVm.Show(
            Named(new Game(), "0100000000010000", "A", size: 1024),
            Named(new Update(), "0100000000010800", "A update", size: 1024),
            Named(new DLC(), "0100000000011001", "A DLC"));
        switchVm.SetUnrecognized("x.nsp");
        var other = new TestConsoleViewModel("Other");
        other.Show(Named(new Game(), "G1", "B"), Named(new Game(), "G1", "B copy"));

        var dashboard = new DashboardViewModel([switchVm, other], _ => { });
        dashboard.Recalculate();

        dashboard.TotalRoms.Should().Be(5);
        dashboard.Games.Should().Be(3);
        dashboard.Updates.Should().Be(1);
        dashboard.Dlcs.Should().Be(1);
        dashboard.Duplicates.Should().Be(2);
        dashboard.NotRecognised.Should().Be(1);
        dashboard.TotalSize.Should().Be(RomManagerShared.Utils.FileUtils.FormatFileSize(2048));
        dashboard.HasConsoles.Should().BeTrue();
    }

    [Fact]
    public void Dashboard_OpenCommandForwardsTheConsole()
    {
        var vm = new TestConsoleViewModel();
        ConsoleViewModelBase? opened = null;
        var dashboard = new DashboardViewModel([vm], c => opened = c);

        dashboard.OpenCommand.Execute(vm);

        opened.Should().BeSameAs(vm);
    }
}

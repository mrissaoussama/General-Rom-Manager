using AvaloniaUI.ViewModels;
using AwesomeAssertions;
using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;

namespace RomManagerTests;

public class ConsoleViewModelFilterTests
{
    private sealed class TestConsoleViewModel() : ConsoleViewModelBase("Test", null)
    {
        public void Show(params Rom[] roms) => ShowRoms(roms);
        protected override Task LoadAsync(bool fullRescan) => Task.CompletedTask;
        public override IReadOnlyList<(string PropName, string Header, bool UsesDictionaryLookup)> GetColumnDescriptors() => [];
        public override IEnumerable<IConsoleRomAction> GetContextActionsForSelection(IEnumerable<Rom> selectedRoms) => [];
        public override IReadOnlyList<ConsoleRequirement> GetAllRequirements() => [];
    }

    private static Game Game(string id, string title, string path, Region region = Region.Unknown, params Language[] languages)
    {
        var game = new Game { TitleID = id, Path = path, Regions = region == Region.Unknown ? null : [region] };
        game.AddTitleName(title);
        foreach (var l in languages) game.AddLanguage(l);
        return game;
    }

    private static List<string> Shown(ConsoleViewModelBase vm) =>
        vm.RomsView.Cast<RomViewModel>().Select(r => r.Title).ToList();

    private static TestConsoleViewModel Library()
    {
        var vm = new TestConsoleViewModel();
        vm.Show(
            Game("A", "Alpha", @"C:\roms\0100 alpha.nsp", Region.USA, Language.English),
            Game("B", "Beta", @"C:\roms\0101 beta.xci", Region.Japan, Language.Japanese, Language.English),
            Game("C", "Gamma", @"C:\roms\0102 gamma.NSP", Region.Europe, Language.French),
            Game("C", "Gamma copy", @"C:\roms\0102 gamma (1).nsp", Region.Europe),
            Game("", "delta", @"C:\roms\delta.nsz"));
        return vm;
    }

    [Fact]
    public void Options_ListValuesPresentInTheLibrary()
    {
        var vm = Library();

        vm.FormatOptions.Should().Equal("All", "NSP", "NSZ", "XCI");
        vm.LanguageOptions.Should().Equal("All", "English", "French", "Japanese");
        vm.RegionOptions.Should().Equal("All", "Europe", "Japan", "Unknown", "USA");
    }

    [Fact]
    public void Filters_Combine()
    {
        var vm = Library();

        vm.FormatFilter = "NSP";
        Shown(vm).Should().Equal("Alpha", "Gamma", "Gamma copy");

        vm.LanguageFilter = "English";
        Shown(vm).Should().Equal("Alpha");
        vm.IsFiltered.Should().BeTrue();

        vm.ClearFiltersCommand.Execute(null);
        Shown(vm).Should().HaveCount(5);
        vm.IsFiltered.Should().BeFalse();
    }

    [Fact]
    public void Filters_UnnamedDuplicatesAndSearch()
    {
        var vm = Library();

        vm.UnnamedOnly = true;
        Shown(vm).Should().Equal("delta");
        vm.UnnamedOnly = false;

        vm.DuplicatesOnly = true;
        Shown(vm).Should().Equal("Gamma", "Gamma copy");
        vm.DuplicateCount.Should().Be(2);
        vm.DuplicatesOnly = false;

        vm.SearchText = "bet";
        Shown(vm).Should().Equal("Beta");
    }

    [Fact]
    public void Filter_ResetsWhenItsValueLeavesTheLibrary()
    {
        var vm = Library();
        vm.FormatFilter = "XCI";

        vm.Show(Game("A", "Alpha", @"C:\roms\alpha.nsp"));

        vm.FormatFilter.Should().Be("All");
        Shown(vm).Should().Equal("Alpha");
    }
}

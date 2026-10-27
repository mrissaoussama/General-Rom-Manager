using AvaloniaUI.Utils;
using AvaloniaUI.ViewModels;
using AwesomeAssertions;
using RomManagerShared.Base.Library;

namespace RomManagerTests;

public class OnboardingViewModelTests
{
    private sealed class Roots(params Type[] configured) : ILibraryRoots
    {
        public IReadOnlyList<string> GetRoots(Type console) => configured.Contains(console) ? ["/roms"] : [];
        public IReadOnlyList<Type> ConfiguredConsoles() => configured;
    }

    [Fact]
    public void ShouldShow_OnlyOnATrueFirstRun()
    {
        OnboardingViewModel.ShouldShow(new Settings(), new Roots()).Should().BeTrue();
        OnboardingViewModel.ShouldShow(new Settings { OnboardingCompleted = true }, new Roots()).Should().BeFalse();
        // Folders already in config.json count as configured.
        OnboardingViewModel.ShouldShow(new Settings(), new Roots(typeof(RomManagerShared.Interfaces.NESConsole))).Should().BeFalse();
    }

    [Fact]
    public void Steps_FinishSavesTheFolders()
    {
        var settings = new Settings();
        var saved = 0;
        var vm = new OnboardingViewModel(settings, _ => saved++);
        vm.BackCommand.CanExecute(null).Should().BeFalse();
        vm.ConfirmCommand.CanExecute(null).Should().BeFalse();

        vm.NextCommand.Execute(null);
        vm.IsFolders.Should().BeTrue();
        vm.Folders.First(f => f.Id == "NES").Path.Value = "/roms/nes";
        vm.NextCommand.Execute(null);
        vm.NextCommand.Execute(null);

        vm.IsDone.Should().BeTrue();
        vm.ChosenFolders.Should().Be(1);
        vm.NextCommand.CanExecute(null).Should().BeFalse();
        vm.ConfirmCommand.Execute(null);
        vm.OnClosed();

        settings.ConsoleLibraryPaths.Should().ContainSingle().Which.Value.Should().Be("/roms/nes");
        settings.OnboardingCompleted.Should().BeTrue();
        saved.Should().Be(1);
    }

    [Fact]
    public void Closing_EarlyKeepsFoldersButDoesNotAskAgain()
    {
        var settings = new Settings();
        var vm = new OnboardingViewModel(settings, _ => { });
        vm.NextCommand.Execute(null);
        vm.Folders[0].Path.Value = "/x";

        vm.OnClosed();

        settings.ConsoleLibraryPaths.Should().BeEmpty();
        settings.OnboardingCompleted.Should().BeTrue();
    }
}

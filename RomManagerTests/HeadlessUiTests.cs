using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using AvaloniaUI;
using AvaloniaUI.ViewModels;
using AvaloniaUI.Views;
using AvaloniaUI.Views.DialogViews;
using AwesomeAssertions;
using RomManagerShared.Base;
using RomManagerShared.Base.Library;
using RomManagerShared.Interfaces;

namespace RomManagerTests;

/// <summary>Avalonia app for headless tests: the real theme and view locator, none of App's startup work.</summary>
public sealed class HeadlessTestApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(new Uri("avares://RomManagerTests")) { Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml") });
        DataTemplates.Add(new ViewLocator());
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<HeadlessTestApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions()).WithInterFont();
}

/// <summary>Renders real views on the headless platform. All UI work runs on the session's UI thread.</summary>
public sealed class HeadlessUiTests
{
    private static readonly Lazy<HeadlessUnitTestSession> Session =
        new(() => HeadlessUnitTestSession.StartNew(typeof(HeadlessTestApp), AvaloniaTestIsolationLevel.PerAssembly));

    private static Task Ui(Action action) =>
        Session.Value.Dispatch(action, TestContext.Current.CancellationToken);

    private sealed class TestConsoleViewModel() : ConsoleViewModelBase("Test", null)
    {
        public void Show(params Rom[] roms) => ShowRoms(roms);
        protected override Task LoadAsync(bool fullRescan) => Task.CompletedTask;
        public override IReadOnlyList<(string PropName, string Header, bool UsesDictionaryLookup)> GetColumnDescriptors() =>
            [("TitleID", "Title ID", false), ("Title", "Title", false)];
        public override IEnumerable<IConsoleRomAction> GetContextActionsForSelection(IEnumerable<Rom> selectedRoms) => [];
        public override IReadOnlyList<ConsoleRequirement> GetAllRequirements() => [];
    }

    private static T Find<T>(Visual root, Func<T, bool>? where = null) where T : Visual =>
        root.GetVisualDescendants().OfType<T>().First(c => where?.Invoke(c) ?? true);

    // A real pointer click in the middle of the button, so its Command binding is exercised.
    private static void Click(Button button)
    {
        var window = (Window)TopLevel.GetTopLevel(button)!;
        window.UpdateLayout();
        var center = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
        window.MouseDown(center, MouseButton.Left);
        window.MouseUp(center, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    [Fact]
    public Task ConfirmDialog_RendersAndClosesWithTheAnswer() => Ui(() =>
    {
        var vm = new ConfirmDialogViewModel("Delete", "Move 2 files to the trash?", "Move to trash");
        var window = new DialogWindow(vm);
        window.Show();

        window.Title.Should().Be("Delete");
        Find<ConfirmDialogView>(window).Should().NotBeNull();
        Find<TextBlock>(window, t => t.Text == "Move 2 files to the trash?").Should().NotBeNull();

        Click(Find<Button>(window, b => b.Content as string == "Move to trash"));

        vm.Confirmed.Should().BeTrue();
        window.IsVisible.Should().BeFalse();
    });

    [Fact]
    public Task PreviewDialog_ListsEveryLineAndCancels() => Ui(() =>
    {
        var vm = new PreviewDialogViewModel("Move", "Move 3 files", ["a → b", "c → d", "e → f"], "Move");
        var window = new DialogWindow(vm);
        window.Show();

        Find<ListBox>(window).ItemCount.Should().Be(3);
        Click(Find<Button>(window, b => b.Content as string == "Cancel"));

        vm.Confirmed.Should().BeFalse();
        window.IsVisible.Should().BeFalse();
    });

    [Fact]
    public Task RenameDialog_ChipInsertsIntoTheTemplateBox() => Ui(() =>
    {
        var rom = new Game { Path = Path.Combine(Path.GetTempPath(), "old.nes"), TitleID = "1" };
        rom.AddTitleName("New");
        var vm = new RenameDialogViewModel([rom], [], "NES", "{TitleName}");
        var window = new DialogWindow(vm);
        window.Show();

        var box = Find<TextBox>(window);
        box.CaretIndex = box.Text!.Length;
        Click(Find<Button>(window, b => b.Content as string == "{TitleID}"));

        box.Text.Should().Be("{TitleName}{TitleID}");
        Find<RenameDialogView>(window).Should().NotBeNull();
    });

    [Fact]
    public Task ChooseConsoleDialog_AddIsDisabledUntilAConsoleIsPicked() => Ui(() =>
    {
        var nes = new ConsoleDescriptor(typeof(object), "NES", "Nintendo Entertainment System");
        var vm = new ChooseConsoleDialogViewModel("D:/roms", [nes], null, "No known ROM files found.");
        var window = new DialogWindow(vm);
        window.Show();

        var add = Find<Button>(window, b => b.Content as string == "Add");
        add.IsEffectivelyEnabled.Should().BeFalse();

        Find<ComboBox>(window).SelectedIndex = 0;

        vm.Selected.Should().Be(nes);
        add.IsEffectivelyEnabled.Should().BeTrue();
    });

    [Fact]
    public Task DashboardView_Renders() => Ui(() =>
    {
        var window = new Window { Content = new DashboardViewModel([], _ => { }) };
        window.Show();

        Find<DashboardView>(window).Should().NotBeNull();
    });

    [Fact]
    public Task ConsoleView_ShowsTheRomsInTheGrid() => Ui(() =>
    {
        var vm = new TestConsoleViewModel();
        var a = new Game { Path = Path.Combine(Path.GetTempPath(), "a.nes"), TitleID = "A" };
        a.AddTitleName("Alpha");
        var b = new Game { Path = Path.Combine(Path.GetTempPath(), "b.nes"), TitleID = "B" };
        b.AddTitleName("Beta");
        vm.Show(a, b);
        var window = new Window { Width = 1200, Height = 800, Content = vm };
        window.Show();

        Find<ConsoleView>(window).Should().NotBeNull();
        var grid = Find<DataGrid>(window);
        grid.ItemsSource.Cast<object>().Should().HaveCount(2);
    });
}

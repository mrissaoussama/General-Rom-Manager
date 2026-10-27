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

[assembly: AssemblyFixture(typeof(RomManagerTests.HeadlessSession))]

namespace RomManagerTests;

/// <summary>
/// One headless Avalonia session for the whole test run, started before any test: Avalonia 12 binds its UI
/// thread to the first thread that touches it, so a view-model test creating a brush first would otherwise
/// leave the headless tests on the wrong thread.
/// </summary>
public sealed class HeadlessSession : IDisposable
{
    private static readonly Lazy<HeadlessUnitTestSession> Session =
        new(() => HeadlessUnitTestSession.StartNew(typeof(HeadlessTestApp), AvaloniaTestIsolationLevel.PerAssembly));

    public static HeadlessUnitTestSession Instance => Session.Value;

    // The first dispatch sets the platform up on the session's thread, before any other test can claim it.
    public HeadlessSession() => Instance.Dispatch(() => { }, CancellationToken.None).GetAwaiter().GetResult();

    public void Dispose() { }
}

/// <summary>Avalonia app for headless tests: the real theme and view locator, none of App's startup work.</summary>
public sealed class HeadlessTestApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(new Uri("avares://RomManagerTests")) { Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml") });
        Styles.Add(new StyleInclude(new Uri("avares://RomManagerTests")) { Source = new Uri("avares://AvaloniaUI/AppStyles.axaml") });
        DataTemplates.Add(new ViewLocator());
    }

    // Skia renders real frames, so tests can also capture screenshots (ScreenshotTests).
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<HeadlessTestApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .WithInterFont();
}

/// <summary>Renders real views on the headless platform. All UI work runs on the session's UI thread.</summary>
public sealed class HeadlessUiTests
{
    private static Task Ui(Action action) =>
        HeadlessSession.Instance.Dispatch(action, TestContext.Current.CancellationToken);

    private sealed class TestConsoleViewModel() : ConsoleViewModelBase("Test", null)
    {
        public void Show(params Rom[] roms) => ShowRoms(roms);
        protected override Task LoadAsync(bool fullRescan) => Task.CompletedTask;
        public override IReadOnlyList<(string PropName, string Header, bool UsesDictionaryLookup)> GetColumnDescriptors() =>
            [("TitleID", "Title ID", false), ("Title", "Title", false), ("RomType", "Type", false),
             ("Region", "Region", false), ("Version", "Version", false)];
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
        // Hit testing works on the rendered scene, so render a frame first.
        Avalonia.Headless.AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        var center = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
        window.MouseMove(center);
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

    [Fact]
    public Task CardView_ArrowKeysMoveFocusBetweenCards() => Ui(() =>
    {
        var vm = new TestConsoleViewModel { IsCardView = true };
        var roms = Enumerable.Range(0, 12).Select(i =>
        {
            var g = new Game { Path = Path.Combine(Path.GetTempPath(), $"{i:D2}.nes"), TitleID = $"{i:D2}" };
            g.AddTitleName($"Game {i:D2}");
            return (Rom)g;
        }).ToArray();
        vm.Show(roms);
        var window = new Window { Width = 800, Height = 900, Content = vm };
        window.Show();
        window.UpdateLayout();

        var cards = Find<ItemsRepeater>(window);
        var first = (Button)cards.GetOrCreateElement(0);
        first.Focus();
        string Focused() => ((RomViewModel)((Control)window.FocusManager!.GetFocusedElement()!).DataContext!).TitleID;

        window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
        Focused().Should().Be("01");

        var columns = cards.Children.Count(c => c.IsVisible && Math.Abs(c.Bounds.Y - first.Bounds.Y) < 1);
        window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        Focused().Should().Be($"{1 + columns:D2}");

        window.KeyPress(Key.End, RawInputModifiers.None, PhysicalKey.End, null);
        Focused().Should().Be("11");
    });

    // ── Performance ──────────────────────────────────────────────────────
    // A 5,000-rom library must load, filter and switch to cards without stalling the UI thread.

    [Fact]
    public Task LargeLibrary_StaysResponsive() => Ui(() =>
    {
        var roms = Enumerable.Range(0, 5000).Select(i =>
        {
            var g = new Game { Path = $"/roms/{i:D5}.nsp", TitleID = $"{i:X16}", Version = "1", Size = 1 << 20 };
            g.AddTitleName($"Game {i:D5}");
            g.AddRegion(i % 2 == 0 ? Region.USA : Region.Japan);
            return (Rom)g;
        }).ToArray();
        var vm = new TestConsoleViewModel();
        var window = new Window { Width = 1400, Height = 900, Content = vm };
        window.Show();

        var sw = System.Diagnostics.Stopwatch.StartNew();
        vm.Show(roms);
        window.UpdateLayout();
        var load = sw.ElapsedMilliseconds;

        sw.Restart();
        vm.SearchText = "Game 049";
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        var filter = sw.ElapsedMilliseconds;
        vm.SearchText = "";
        Dispatcher.UIThread.RunJobs();

        sw.Restart();
        vm.IsCardView = true;
        window.UpdateLayout();
        var cards = sw.ElapsedMilliseconds;
        var realized = Find<ItemsRepeater>(window).Children.Count(c => c.IsVisible);

        if (Environment.GetEnvironmentVariable("PERF_LOG") is { } perfLog) File.WriteAllText(perfLog, $"load {load} ms, filter {filter} ms, cards {cards} ms, {realized} cards realized");
        realized.Should().BeLessThan(200, "cards are virtualized");
        load.Should().BeLessThan(5000);
        filter.Should().BeLessThan(2000);
        cards.Should().BeLessThan(3000);
    });

    // ── Screenshots ──────────────────────────────────────────────────────
    // Always renders (a cheap check that real frames work); writes docs/screenshots/*.png with SCREENSHOTS=1.

    private static TestConsoleViewModel SampleConsole(Type console, params (string Title, string Id, Region Region)[] games)
    {
        var vm = new TestConsoleViewModel();
        vm.Describe(RomManagerShared.Base.ConsoleModules.Describe(console));
        vm.Show([.. games.Select(g =>
        {
            var rom = new Game { Path = Path.Combine(Path.GetTempPath(), "roms", g.Title + ".bin"), TitleID = g.Id, Version = "1.0", Size = 4L << 20 };
            rom.AddTitleName(g.Title);
            rom.AddRegion(g.Region);
            return (Rom)rom;
        })]);
        return vm;
    }

    private static void Capture(Window window, string name, [System.Runtime.CompilerServices.CallerFilePath] string caller = "")
    {
        window.UpdateLayout();
        var frame = window.CaptureRenderedFrame();
        frame.Should().NotBeNull();
        if (Environment.GetEnvironmentVariable("SCREENSHOTS") != "1") return;
        var dir = Path.Combine(Path.GetDirectoryName(caller)!, "..", "docs", "screenshots");
        Directory.CreateDirectory(dir);
        frame!.Save(Path.Combine(dir, name + ".png"), new Avalonia.Media.Imaging.PngBitmapEncoderOptions());
    }

    [Fact]
    public Task Screenshots() => Ui(() =>
    {
        var nes = SampleConsole(typeof(NESConsole),
            ("Star Courier", "A1", Region.USA), ("Pixel Quest", "A2", Region.Europe), ("Lunar Drift", "A3", Region.Japan),
            ("Tiny Knight", "A4", Region.USA), ("Rocket Mole", "A5", Region.World), ("Neon Garden", "A6", Region.USA));
        var gb = SampleConsole(typeof(GameBoyConsole),
            ("Ghost Lantern", "B1", Region.USA), ("Puzzle Pond", "B2", Region.Europe), ("Sky Ferry", "B3", Region.Japan));
        var vm = new DashboardViewModel([nes, gb], _ => { }) { DatDirectory = Path.Combine("RomManager", "Data", "DAT") };
        vm.RefreshDats();
        Dispatcher.UIThread.RunJobs(); // the totals are computed at background priority
        var dashboard = new Window { Width = 1100, Height = 650, Content = vm };
        dashboard.Show();
        vm.TotalRoms.Should().Be(9);
        Capture(dashboard, "dashboard");

        var table = new Window { Width = 1100, Height = 500, Content = nes };
        table.Show();
        Capture(table, "console-table");

        nes.IsCardView = true; // same window: the ViewLocator keeps one view per page
        Capture(table, "console-cards");

        var settings = new SettingsViewModel(new AvaloniaUI.Utils.Settings(), consoles: [nes, gb], persist: _ => { });
        var settingsWindow = new DialogWindow(settings);
        settingsWindow.Show();
        Find<SettingsView>(settingsWindow).Should().NotBeNull();
        Capture(settingsWindow, "settings");
        var tabs = Find<TabControl>(settingsWindow);
        tabs.SelectedIndex = 1;
        settingsWindow.UpdateLayout();
        Find<CheckBox>(settingsWindow, c => c.Content as string == "Title").IsChecked.Should().BeTrue();
        tabs.SelectedIndex = 2;
        settingsWindow.UpdateLayout();
        Find<Button>(settingsWindow, b => b.Content as string == "Reload plugins").Should().NotBeNull();
        settingsWindow.Close();

        var onboarding = new OnboardingViewModel(new AvaloniaUI.Utils.Settings(), _ => { });
        onboarding.NextCommand.Execute(null);
        var onboardingWindow = new DialogWindow(onboarding);
        onboardingWindow.Show();
        Find<OnboardingView>(onboardingWindow).Should().NotBeNull();
        Capture(onboardingWindow, "onboarding");
        Click(Find<Button>(onboardingWindow, b => b.Content as string == "Next" && b.IsEffectivelyVisible));
        onboarding.IsTheme.Should().BeTrue();
        onboardingWindow.Close();
    });
}

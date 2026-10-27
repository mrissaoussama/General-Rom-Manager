using Avalonia.Controls.Shapes;
using AvaloniaUI.Utils;
using RomManagerShared.Utils;

namespace AvaloniaUI.Views;

/// <summary>
/// First-run setup wizard. Shown when no ROM paths are configured.
/// Steps: Welcome → Select ROM Folders → Choose Theme → Done.
/// </summary>
public sealed class OnboardingWindow : Window
{
    private int _step = 0;
    private const int TotalSteps = 4;

    // Step panels
    private readonly Control[] _steps;

    // Navigation
    private readonly Button _backBtn;
    private readonly Button _nextBtn;
    private readonly TextBlock _stepIndicator;

    // ROM paths entered during wizard
    private readonly Dictionary<string, TextBox> _pathBoxes = new();

    public OnboardingWindow()
    {
        Title = "Welcome to RomManager";
        Width = 600;
        Height = 480;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        // Build all step panels
        _steps = new Control[]
        {
            BuildStepWelcome(),
            BuildStepRomPaths(),
            BuildStepTheme(),
            BuildStepDone(),
        };

        // Navigation bar
        _backBtn = new Button { Content = "← Back", Width = 90, Margin = new Thickness(0, 0, 8, 0) };
        _backBtn.Click += (_, _) => Navigate(-1);

        _nextBtn = new Button { Content = "Next →", Width = 90 };
        _nextBtn.Click += (_, _) => Navigate(+1);

        _stepIndicator = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 11,
            Opacity = 0.6,
        };

        var navBar = new DockPanel { Margin = new Thickness(16, 8) };
        DockPanel.SetDock(_nextBtn, Dock.Right);
        navBar.Children.Add(_nextBtn);
        DockPanel.SetDock(_backBtn, Dock.Right);
        navBar.Children.Add(_backBtn);
        navBar.Children.Add(_stepIndicator);

        // Content area (swaps between step panels)
        var contentBorder = new Border
        {
            Margin = new Thickness(0, 0, 0, 0),
            Child = _steps[0],
        };

        var root = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(new Separator { Margin = new Thickness(0) }, Dock.Bottom);

        var navBorder = new Border { Child = navBar, BorderThickness = new Thickness(0, 1, 0, 0) };
        DockPanel.SetDock(navBorder, Dock.Bottom);
        root.Children.Add(navBorder);
        root.Children.Add(contentBorder);

        Content = root;
        _contentBorder = contentBorder;
        UpdateNav();
    }

    private readonly Border _contentBorder;

    private void Navigate(int delta)
    {
        // If already on the final (Done) step and user clicks Finish → save & close
        if (_step == TotalSteps - 1 && delta == +1)
        {
            SaveRomPaths();
            Close();
            return;
        }

        // On leaving the ROM Paths step (step 1), persist entered paths
        if (_step == 1 && delta == 1)
            SaveRomPaths();

        _step = Math.Clamp(_step + delta, 0, TotalSteps - 1);
        _contentBorder.Child = _steps[_step];
        UpdateNav();
    }

    private void UpdateNav()
    {
        _backBtn.IsVisible = _step > 0;
        _stepIndicator.Text = $"Step {_step + 1} of {TotalSteps}";
        _nextBtn.Content = _step == TotalSteps - 1 ? "✅ Finish" : "Next →";
    }

    private void SaveRomPaths()
    {
        foreach (var kv in _pathBoxes)
        {
            var path = kv.Value.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(path))
                Settings.Instance.ConsoleLibraryPaths[kv.Key] = path;
            else
                Settings.Instance.ConsoleLibraryPaths.Remove(kv.Key);
        }
        Settings.Instance.Save();
    }

    // ── Step 0: Welcome ──────────────────────────────────────────────────

    private static Control BuildStepWelcome()
    {
        var sp = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 16,
            Margin = new Thickness(48),
        };

        sp.Children.Add(new TextBlock
        {
            Text = "Welcome to RomManager",
            FontSize = 26,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
        });

        sp.Children.Add(new TextBlock
        {
            Text = "RomManager helps you organise, rename, and manage your ROM collection across all major consoles.",
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = 14,
            Opacity = 0.7,
            TextAlignment = TextAlignment.Center,
        });

        sp.Children.Add(new TextBlock
        {
            Text = "This quick setup will help you configure your ROM library paths.\nYou can change everything later in Settings.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            Opacity = 0.6,
            TextAlignment = TextAlignment.Center,
        });

        return new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }

    // ── Step 1: ROM Paths ────────────────────────────────────────────────

    private Control BuildStepRomPaths()
    {
        var sp = new StackPanel { Margin = new Thickness(24), Spacing = 6 };

        sp.Children.Add(new TextBlock
        {
            Text = "ROM Library Paths",
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 4),
        });

        sp.Children.Add(new TextBlock
        {
            Text = "Enter the folder path for each console you want to manage. Leave blank to skip.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            Opacity = 0.6,
            Margin = new Thickness(0, 0, 0, 10),
        });

        var popularConsoles = ConsoleModules.Descriptors.Select(d => (d.Id, d.DisplayName));

        foreach (var (key, label) in popularConsoles)
        {
            var capturedKey = key;
            var existing = Settings.Instance.ConsoleLibraryPaths.TryGetValue(key, out var v) ? v : "";

            sp.Children.Add(new TextBlock { Text = label, FontSize = 12, FontWeight = FontWeight.SemiBold });

            var box = new TextBox { Text = existing, Watermark = "Path to ROM folder…", HorizontalAlignment = HorizontalAlignment.Stretch };
            _pathBoxes[capturedKey] = box;

            var btn = new Button { Content = "Browse…", Padding = new Thickness(8, 3), Margin = new Thickness(4, 0, 0, 0) };
            var capturedBox = box;
            btn.Click += async (_, _) =>
            {
                var result = await Dialogs.PickFolderAsync(this, $"Select {label} ROM folder");
                if (result != null) capturedBox.Text = result;
            };

            var row = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
            DockPanel.SetDock(btn, Dock.Right);
            row.Children.Add(btn);
            row.Children.Add(box);
            sp.Children.Add(row);
        }

        return new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }

    // ── Step 2: Theme ───────────────────────────────────────────────────

    private static Control BuildStepTheme()
    {
        var sp = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 14,
            Margin = new Thickness(48),
        };

        sp.Children.Add(new TextBlock
        {
            Text = "Choose a Theme",
            FontSize = 18,
            FontWeight = FontWeight.Bold,
        });

        sp.Children.Add(new TextBlock
        {
            Text = "Pick an interface colour theme. You can change it any time in View → Theme.",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.6,
        });

        foreach (var (key, label, icon) in new[]
        {
            ("Default", "System Default (follows OS)",    "⚙️ "),
            ("Light",   "Light",                          "☀️ "),
            ("Dark",    "Dark",                           "🌙 "),
        })
        {
            var capturedKey = key;
            var radioPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            var radio = new RadioButton
            {
                Content = icon + label,
                GroupName = "ThemeGroup",
                IsChecked = Settings.Instance.Theme == capturedKey,
                FontSize = 14,
            };
            radio.IsCheckedChanged += (_, _) =>
            {
                if (radio.IsChecked != true) return;
                Settings.Instance.Theme = capturedKey;
                Settings.Instance.Save();
                if (Application.Current is not null)
                {
                    Application.Current.RequestedThemeVariant = capturedKey switch
                    {
                        "Dark"  => Avalonia.Styling.ThemeVariant.Dark,
                        "Light" => Avalonia.Styling.ThemeVariant.Light,
                        _       => Avalonia.Styling.ThemeVariant.Default,
                    };
                }
            };
            sp.Children.Add(radio);
        }

        return new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }

    // ── Step 3: Done ────────────────────────────────────────────────────

    private static Control BuildStepDone()
    {
        var sp = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 16,
            Margin = new Thickness(48),
        };

        sp.Children.Add(new TextBlock
        {
            Text = "✅ You're all set!",
            FontSize = 24,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
        });

        sp.Children.Add(new TextBlock
        {
            Text = "Your ROM library paths have been saved.\n\n" +
                   "Click Finish to open RomManager. Press the ⟳ Refresh button " +
                   "on any console tab to scan your library.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            Opacity = 0.7,
            TextAlignment = TextAlignment.Center,
        });

        return sp;
    }

    // ── Static helper ────────────────────────────────────────────────────

    /// <summary>
    /// Shows the wizard as a dialog if this appears to be a first run
    /// (no ROM paths configured yet). Safe to call multiple times — only
    /// opens once per application session.
    /// </summary>
    private static bool _hasShownThisSession = false;

    public static async Task ShowIfFirstRunAsync(Window owner)
    {
        if (_hasShownThisSession) return;
        if (Settings.Instance.ConsoleLibraryPaths.Count > 0) return;

        _hasShownThisSession = true;
        var wizard = new OnboardingWindow();
        await wizard.ShowDialog(owner);
    }
}

using Avalonia.Controls.Shapes;
using AvaloniaUI.ViewModels;
using RomManagerShared.Configuration;
using RomManagerShared.Utils;

using RomManagerShared.Switch.Configuration;

namespace AvaloniaUI.Views;

/// <summary>
/// Compact settings dialog with three tabs:
///   1. General  – paths, rename format
///   2. Columns  – per-console column visibility
///   3. Plugins  – enable/disable loaded plugins
/// </summary>
public sealed class SettingsWindow : Window
{
    private readonly PluginManager? _plugins;
    private readonly IEnumerable<IConsoleViewModel>? _consoleViewModels;
    // Supplied by MainViewModel: queues missing title DB downloads, returns count queued (-1 = check failed).
    private readonly Func<Task<int>>? _downloadTitleDatabases;

    public SettingsWindow(PluginManager? plugins = null, IEnumerable<IConsoleViewModel>? consoleViewModels = null,
        Func<Task<int>>? downloadTitleDatabases = null)
    {
        _plugins = plugins;
        _consoleViewModels = consoleViewModels;
        _downloadTitleDatabases = downloadTitleDatabases;
        Title = "Settings";
        Width = 680;
        Height = 480;
        CanResize = true;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        Content = BuildContent();
    }

    // ── Layout ────────────────────────────────────────────────────────────

    private Control BuildContent()
    {
        var tabs = new TabControl();
        tabs.Items.Add(new TabItem { Header = "General",  Content = BuildGeneralTab() });
        tabs.Items.Add(new TabItem { Header = "Columns",  Content = BuildColumnsTab() });
        tabs.Items.Add(new TabItem { Header = "Plugins",  Content = BuildPluginsTab() });

        var root = new DockPanel { LastChildFill = true };

        // Bottom button bar
        var btnBar = BuildButtonBar();
        DockPanel.SetDock(btnBar, Dock.Bottom);
        root.Children.Add(btnBar);
        root.Children.Add(tabs);

        return root;
    }

    // ── General tab ───────────────────────────────────────────────────────

    private Control BuildGeneralTab()
    {
        var settings = Settings.Instance;
        var sp = new StackPanel { Margin = new Thickness(12), Spacing = 10 };

        // ── Appearance ────────────────────────────────────────────────────
        sp.Children.Add(MakeLabel("Appearance", fontSize: 14, bold: true));

        sp.Children.Add(MakeLabel("Colour theme"));
        var themeCombo = new ComboBox
        {
            Width = 200,
            ItemsSource = new[] { "Default (system)", "Light", "Dark" },
            SelectedItem = settings.Theme switch
            {
                "Light" => "Light",
                "Dark"  => "Dark",
                _       => "Default (system)",
            },
        };
        themeCombo.SelectionChanged += (_, _) =>
        {
            var key = themeCombo.SelectedItem?.ToString() switch
            {
                "Light" => "Light",
                "Dark"  => "Dark",
                _       => "Default",
            };
            settings.Theme = key;
            // Apply immediately
            if (Application.Current is not null)
            {
                Application.Current.RequestedThemeVariant = key switch
                {
                    "Dark"  => Avalonia.Styling.ThemeVariant.Dark,
                    "Light" => Avalonia.Styling.ThemeVariant.Light,
                    _       => Avalonia.Styling.ThemeVariant.Default,
                };
            }
        };
        sp.Children.Add(themeCombo);

        var compactCheck = new CheckBox
        {
            Content = "Compact mode (smaller row height)",
            IsChecked = settings.IsCompactMode,
        };
        compactCheck.IsCheckedChanged += (_, _) =>
        {
            settings.IsCompactMode = compactCheck.IsChecked ?? false;
            Settings.RaiseCompactModeChanged();
        };
        sp.Children.Add(compactCheck);

        sp.Children.Add(MakeSeparator());

        // ── Paths ─────────────────────────────────────────────────────────
        sp.Children.Add(MakeLabel("Paths", fontSize: 14, bold: true));

        var dbPathBox = MakeTextBox(settings.DatabasePath ?? "", w: 310);
        dbPathBox.TextChanged += (_, _) => { settings.DatabasePath = dbPathBox.Text; };
        var dbResetBtn = new Button
        {
            Content = "Reset to default",
            Padding = new Thickness(6, 2),
            Margin = new Thickness(4, 0, 0, 0),
        };
        ToolTip.SetTip(dbResetBtn, "Clear the override — the path from config.json will be used");
        dbResetBtn.Click += (_, _) => { dbPathBox.Text = ""; settings.DatabasePath = null; };
        sp.Children.Add(MakeLabeledPathRow("Database / metadata", dbPathBox, "Browse…", async () =>
        {
            var result = await Dialogs.PickFolderAsync(this, "Select database directory");
            if (result != null) dbPathBox.Text = result;
        }, extraButton: dbResetBtn, status: MakePathStatus(dbPathBox, emptyText: "Not set (default)")));

        // ── ROM Library Paths (per-console) ────────────────────────────────
        sp.Children.Add(MakeSeparator());
        sp.Children.Add(MakeLabel("ROM Library Paths", fontSize: 14, bold: true));
        sp.Children.Add(MakeLabel(
            "For each console, choose the folder that contains your ROM files.",
            fontSize: 11, muted: true));

        var knownConsoles = ConsoleModules.Descriptors.Select(d => d.Id);

        foreach (var consoleName in knownConsoles)
        {
            var capturedName = consoleName;
            var currentPath = settings.ConsoleLibraryPaths.TryGetValue(capturedName, out var cp) ? cp : "";

            var pathBox = MakeTextBox(currentPath, w: 300);
            pathBox.TextChanged += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(pathBox.Text))
                    settings.ConsoleLibraryPaths.Remove(capturedName);
                else
                    settings.ConsoleLibraryPaths[capturedName] = pathBox.Text ?? "";
            };
            var capturedBox = pathBox;
            sp.Children.Add(MakeLabeledPathRow(capturedName, capturedBox, "Browse…", async () =>
            {
                var result = await Dialogs.PickFolderAsync(this, $"Select {capturedName} ROM folder");
                if (result != null) capturedBox.Text = result;
            }, status: MakePathStatus(capturedBox)));
        }

        sp.Children.Add(MakeSeparator());

        // ── File Organiser ────────────────────────────────────────────────
        sp.Children.Add(MakeLabel("File Organiser", fontSize: 14, bold: true));

        sp.Children.Add(MakeLabel("Rename format string"));
        sp.Children.Add(MakeLabel(
            "Placeholders: {TitleName} {TitleID} {Region} {Version} {DLCCount} {Publisher} {Developer} {ReleaseDate} {Platform}",
            fontSize: 11, muted: true));

        var formatBox = MakeTextBox(settings.RenameFormatString, w: 480);
        formatBox.TextChanged += (_, _) => { settings.RenameFormatString = formatBox.Text ?? ""; };
        sp.Children.Add(formatBox);

        // Placeholder chips — click to insert at end of format string
        var chipsPanel = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
        sp.Children.Add(MakeLabel("Insert placeholder:", fontSize: 11, muted: true));

        foreach (var placeholder in new[]
        {
            "{TitleName}", "{TitleID}", "{Region}", "{Version}",
            "{Publisher}", "{Developer}", "{ReleaseDate}", "{Platform}"
        })
        {
            var p = placeholder; // capture
            var chip = new Button
            {
                Content = p,
                Padding = new Thickness(8, 3),
                Margin = new Thickness(0, 0, 4, 4),
                FontSize = 11,
            };
            chip.Click += (_, _) =>
            {
                var pos = formatBox.CaretIndex;
                var text = formatBox.Text ?? "";
                formatBox.Text = text.Insert(pos, p);
                formatBox.CaretIndex = pos + p.Length;
            };
            chipsPanel.Children.Add(chip);
        }
        sp.Children.Add(chipsPanel);

        sp.Children.Add(MakeSeparator());

        // ── Downloads ─────────────────────────────────────────────────────
        sp.Children.Add(MakeLabel("Downloads", fontSize: 14, bold: true));

        var autoDlCheck = new CheckBox
        {
            Content = "Auto-download missing database files on startup",
            IsChecked = settings.AutoDownloadMissingDBs,
        };
        autoDlCheck.IsCheckedChanged += (_, _) =>
        {
            settings.AutoDownloadMissingDBs = autoDlCheck.IsChecked ?? false;
        };
        sp.Children.Add(autoDlCheck);

        // Reuses MainViewModel's missing-resource check + download manager.
        var dlStatus = MakeLabel("", fontSize: 11, muted: true);
        dlStatus.VerticalAlignment = VerticalAlignment.Center;
        var dlBtn = new Button { Content = "Download title databases", IsEnabled = _downloadTitleDatabases is not null };
        ToolTip.SetTip(dlBtn, "Queue downloads for any missing title database files");
        dlBtn.Click += async (_, _) =>
        {
            if (_downloadTitleDatabases is null) return;
            dlBtn.IsEnabled = false;
            dlStatus.Text = "Checking…";
            try
            {
                var queued = await _downloadTitleDatabases();
                dlStatus.Text = queued switch
                {
                    < 0 => "Check failed (see log)",
                    0   => "All present",
                    _   => $"Queued {queued} download(s)",
                };
            }
            catch (Exception ex)
            {
                FileUtils.Log($"[SettingsWindow] Title DB download failed: {ex.Message}");
                dlStatus.Text = "Check failed (see log)";
            }
            finally { dlBtn.IsEnabled = true; }
        };
        sp.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { dlBtn, dlStatus },
        });

        sp.Children.Add(MakeSeparator());

        // ── Switch keys ───────────────────────────────────────────────────
        sp.Children.Add(MakeLabel("Switch Keys", fontSize: 14, bold: true));
        sp.Children.Add(MakeLabel(
            $"Read from {RomManagerShared.Switch.SwitchKeyStatus.DefaultKeysDirectory}. Only counts are shown, never key values.",
            fontSize: 11, muted: true));
        var keysPanel = new StackPanel { Spacing = 2 };
        sp.Children.Add(keysPanel);
        var keysRefreshBtn = new Button { Content = "Refresh", Padding = new Thickness(6, 2) };
        keysRefreshBtn.Click += async (_, _) => await RefreshKeyStatusAsync(keysPanel);
        sp.Children.Add(keysRefreshBtn);
        _ = RefreshKeyStatusAsync(keysPanel);

        sp.Children.Add(MakeSeparator());

        // ── URL / Path Overrides ──────────────────────────────────────────
        sp.Children.Add(MakeLabel("URL / Path Overrides", fontSize: 14, bold: true));
        sp.Children.Add(MakeLabel(
            "Leave blank to use the defaults from config.json. Only set these if you want to point to a custom source.",
            fontSize: 11, muted: true));

        sp.Children.Add(MakeLabel("Switch TitleDB URL"));
        var switchUrlBox = MakeTextBox(settings.SwitchTitleDBUrlOverride ?? "", w: 480);
        switchUrlBox.Watermark = "https://raw.githubusercontent.com/blawar/titledb/master/";
        switchUrlBox.TextChanged += (_, _) =>
        {
            var val = string.IsNullOrWhiteSpace(switchUrlBox.Text) ? null : switchUrlBox.Text;
            settings.SwitchTitleDBUrlOverride = val;
        };
        sp.Children.Add(switchUrlBox);

        sp.Children.Add(MakeLabel("Switch Versions URL"));
        var switchVerBox = MakeTextBox(settings.SwitchVersionsUrlOverride ?? "", w: 480);
        switchVerBox.Watermark = "https://raw.githubusercontent.com/blawar/titledb/master/versions.json";
        switchVerBox.TextChanged += (_, _) =>
        {
            var val = string.IsNullOrWhiteSpace(switchVerBox.Text) ? null : switchVerBox.Text;
            settings.SwitchVersionsUrlOverride = val;
        };
        sp.Children.Add(switchVerBox);

        sp.Children.Add(MakeLabel("Switch TitleDB Local Path (relative)"));
        var switchPathBox = MakeTextBox(settings.SwitchTitleDBPathOverride ?? "", w: 480);
        switchPathBox.Watermark = "titledb";
        switchPathBox.TextChanged += (_, _) =>
        {
            var val = string.IsNullOrWhiteSpace(switchPathBox.Text) ? null : switchPathBox.Text;
            settings.SwitchTitleDBPathOverride = val;
        };
        sp.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Children = { switchPathBox, MakePathStatus(switchPathBox, emptyText: "Not set (default)") },
        });

        sp.Children.Add(MakeSeparator());

        // ── Plugins (path only — the Plugins tab has the list) ────────────
        sp.Children.Add(MakeSeparator());
        sp.Children.Add(MakeLabel("Plugins", fontSize: 14, bold: true));

        var pluginsDirBox = MakeTextBox(settings.PluginsDirectory, w: 360);
        pluginsDirBox.TextChanged += (_, _) => { settings.PluginsDirectory = pluginsDirBox.Text ?? "plugins"; };
        sp.Children.Add(MakeLabeledPathRow("Plugins directory", pluginsDirBox, "Browse…", async () =>
        {
            var result = await Dialogs.PickFolderAsync(this, "Select plugins directory");
            if (result != null) pluginsDirBox.Text = result;
        }, status: MakePathStatus(pluginsDirBox)));

        return new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }

    // ── Columns tab ───────────────────────────────────────────────────────

    private Control BuildColumnsTab()
    {
        var settings = Settings.Instance;
        var sp = new StackPanel { Margin = new Thickness(12), Spacing = 8 };

        sp.Children.Add(new TextBlock
        {
            Text = "Per-console column visibility  —  tick/untick to show or hide columns.",
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
        });
        sp.Children.Add(new TextBlock
        {
            Text = "Changes apply immediately.",
            FontSize = 11,
            Opacity = 0.6,
        });
        sp.Children.Add(new Separator { Margin = new Thickness(0, 2, 0, 4) });

        // Build a lookup: consoleName → IConsoleViewModel so we can get per-console column descriptors.
        var vmLookup = (_consoleViewModels ?? Enumerable.Empty<IConsoleViewModel>())
            .ToDictionary(vm => vm.ConsoleName, vm => vm, StringComparer.OrdinalIgnoreCase);

        // Show all consoles: those from loaded VMs + any already saved in settings.
        var allConsoleNames = vmLookup.Keys
            .Union(settings.VisibleColumns.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n)
            .ToList();

        if (allConsoleNames.Count == 0)
        {
            sp.Children.Add(new TextBlock
            {
                Text = "No consoles loaded yet. Add ROM paths in the General tab first, then re-open Settings.",
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.7,
            });
            return new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        foreach (var consoleName in allConsoleNames)
        {
            var capConsole = consoleName;

            // ── Section header bar ────────────────────────────────────────
            var headerBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 128, 128, 128)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4),
                Margin = new Thickness(0, 4, 0, 2),
            };
            var headerDock = new DockPanel();

            var consoleLbl = new TextBlock
            {
                Text = consoleName,
                FontWeight = FontWeight.SemiBold,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
            };
            DockPanel.SetDock(consoleLbl, Dock.Left);
            headerDock.Children.Add(consoleLbl);

            // Reset link
            var resetBtn = new Button
            {
                Content = "Reset to defaults",
                Padding = new Thickness(6, 1),
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            resetBtn.Click += (_, _) =>
            {
                settings.VisibleColumns[capConsole] = ["TitleID", "Title", "RomType", "Region", "Version"];
                Settings.RaiseColumnsChanged(capConsole);
                settings.Save();
                // Rebuild tab to reflect reset
                Close();
            };
            DockPanel.SetDock(resetBtn, Dock.Right);
            headerDock.Children.Add(resetBtn);

            headerBorder.Child = headerDock;
            sp.Children.Add(headerBorder);

            // ── Column checkboxes ─────────────────────────────────────────
            IReadOnlyList<(string PropName, string Header, bool UsesDictionaryLookup)> descriptors;
            if (vmLookup.TryGetValue(consoleName, out var vm))
            {
                descriptors = vm.GetColumnDescriptors();
            }
            else
            {
                descriptors = typeof(Rom).GetProperties()
                    .Select(p => new { p, attrs = p.GetCustomAttributes(typeof(UiPropertyAttribute), true) })
                    .Where(x => x.attrs.Length > 0 && ((UiPropertyAttribute)x.attrs[0]).Visible)
                    .OrderBy(x => ((UiPropertyAttribute)x.attrs[0]).Order)
                    .Select(x =>
                    {
                        var attr = (UiPropertyAttribute)x.attrs[0];
                        return (x.p.Name,
                                !string.IsNullOrEmpty(attr.DisplayName) ? attr.DisplayName : x.p.Name,
                                false);
                    })
                    .ToList<(string, string, bool)>();
            }

            // Ensure this console has a list in settings.
            if (!settings.VisibleColumns.ContainsKey(consoleName))
                settings.VisibleColumns[consoleName] = ["TitleID", "Title", "RomType", "Region", "Version"];

            var propsPanel = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 0, 0, 4) };

            if (descriptors.Count == 0)
            {
                propsPanel.Children.Add(new TextBlock { Text = "No configurable columns for this console.", FontSize = 11, Opacity = 0.6 });
            }
            else
            {
                foreach (var (propName, header, _) in descriptors)
                {
                    var capProp = propName;
                    var isChecked = settings.VisibleColumns[consoleName].Contains(capProp);

                    var cb = new CheckBox
                    {
                        Content = new TextBlock { Text = header, FontSize = 12 },
                        IsChecked = isChecked,
                        Margin = new Thickness(0, 0, 12, 4),
                    };

                    cb.IsCheckedChanged += (_, _) =>
                    {
                        if (!settings.VisibleColumns.ContainsKey(capConsole))
                            settings.VisibleColumns[capConsole] = new List<string>();

                        var cols = settings.VisibleColumns[capConsole];
                        if (cb.IsChecked == true && !cols.Contains(capProp))
                            cols.Add(capProp);
                        else if (cb.IsChecked == false)
                            cols.Remove(capProp);

                        Settings.RaiseColumnsChanged(capConsole);
                    };

                    propsPanel.Children.Add(cb);
                }
            }

            sp.Children.Add(propsPanel);
        }

        return new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }

    // ── Plugins tab ───────────────────────────────────────────────────────

    private Control BuildPluginsTab()
    {
        var settings = Settings.Instance;
        var root = new DockPanel { LastChildFill = true };

        // ── Toolbar ───────────────────────────────────────────────────────
        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(12, 8, 12, 4),
        };

        var importBtn = new Button { Content = "📦 Import Plugin (.zip)…" };
        importBtn.Click += async (_, _) =>
        {
            var result = await Dialogs.PickFileAsync(this, "Select Plugin ZIP", "Plugin zip", "zip");
            if (result is null) return;

            if (_plugins is null) { return; }
            try
            {
                importBtn.IsEnabled = false;
                var name = await _plugins.ImportZipAsync(result);
                // Rebuild the plugins tab content
                var newContent = BuildPluginsTab();
                Close(); // simplest: close and re-open settings
                _ = name; // used
            }
            catch (Exception ex)
            {
                var dialog = new Window
                {
                    Title = "Import Error",
                    Content = new TextBlock { Text = ex.Message, Margin = new Thickness(16), TextWrapping = TextWrapping.Wrap },
                    Width = 420, Height = 140, CanResize = false,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                await dialog.ShowDialog(this);
            }
            finally { importBtn.IsEnabled = true; }
        };
        toolbar.Children.Add(importBtn);

        var openFolderBtn = new Button { Content = "📁 Open Plugins Folder" };
        openFolderBtn.Click += (_, _) =>
        {
            var dir = RomManagerShared.Configuration.RomManagerConfiguration.GetGlobalPath("Plugins");
            Directory.CreateDirectory(dir);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dir) { UseShellExecute = true });
        };
        toolbar.Children.Add(openFolderBtn);

        var reloadBtn = new Button { Content = "🔄 Reload Plugins" };
        reloadBtn.Click += async (_, _) =>
        {
            if (_plugins is null) return;
            try
            {
                reloadBtn.IsEnabled = false;
                var dir = RomManagerShared.Configuration.RomManagerConfiguration.GetGlobalPath("Plugins");
                await _plugins.LoadPluginsAsync(dir);
                // Re-open settings so the plugin list refreshes
                var ownerWindow = Owner as Window;
                Close();
                var fresh = new SettingsWindow(_plugins, _consoleViewModels, _downloadTitleDatabases);
                if (ownerWindow is not null)
                    await fresh.ShowDialog(ownerWindow);
                else
                    fresh.Show();
            }
            catch (Exception ex)
            {
                FileUtils.Log($"[SettingsWindow] Plugin reload failed: {ex}");
            }
            finally { reloadBtn.IsEnabled = true; }
        };
        toolbar.Children.Add(reloadBtn);

        DockPanel.SetDock(toolbar, Dock.Top);
        root.Children.Add(toolbar);
        DockPanel.SetDock(new Separator { Margin = new Thickness(0, 4, 0, 0) }, Dock.Top);

        // ── Plugin list ───────────────────────────────────────────────────
        var sp = new StackPanel { Margin = new Thickness(12), Spacing = 6 };
        var plugins = _plugins?.Plugins.ToList() ?? [];

        if (plugins.Count == 0)
        {
            sp.Children.Add(MakeLabel("No plugins loaded.", muted: true));
            sp.Children.Add(MakeLabel("Import a .zip to add a plugin, or place a plugin DLL folder in the Plugins directory and restart.", muted: true));
        }
        else
        {
            foreach (var plugin in plugins)
            {

                var row = new Border
                {
                    BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 6)
            };

            var rowGrid = new Grid();
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            var infoPanel = new StackPanel { Spacing = 2 };
            infoPanel.Children.Add(MakeLabel($"{plugin.PluginName}  v{plugin.PluginVersion}", bold: true));
            if (!string.IsNullOrEmpty(plugin.PluginDescription))
                infoPanel.Children.Add(MakeLabel(plugin.PluginDescription, muted: true));
            if (!string.IsNullOrEmpty(plugin.PluginAuthor))
                infoPanel.Children.Add(MakeLabel($"Author: {plugin.PluginAuthor}", fontSize: 11, muted: true));
            if (plugin.SupportedConsoles?.Count > 0)
                infoPanel.Children.Add(MakeLabel(
                    $"Consoles: {string.Join(", ", plugin.SupportedConsoles)}", fontSize: 11, muted: true));

            Grid.SetColumn(infoPanel, 0);
            rowGrid.Children.Add(infoPanel);

            var toggle = new ToggleSwitch
            {
                IsChecked = settings.IsPluginEnabled(plugin.PluginName),
                VerticalAlignment = VerticalAlignment.Center,
                OffContent = "Disabled",
                OnContent = "Enabled"
            };
            toggle.IsCheckedChanged += (_, _) =>
                settings.SetPluginEnabled(plugin.PluginName, toggle.IsChecked ?? true);

            Grid.SetColumn(toggle, 1);
            rowGrid.Children.Add(toggle);

            row.Child = rowGrid;
            sp.Children.Add(row);
        } // end foreach
        } // end else

        root.Children.Add(new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        return root;
    }

    // ── Button bar ────────────────────────────────────────────────────────

    private Control BuildButtonBar()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(8),
            Spacing = 8
        };

        var saveBtn = new Button { Content = "Save & Close" };
        saveBtn.Click += (_, _) =>
        {
            Settings.Instance.Save();
            MainViewModel.ReloadRomPaths(); // pick up any path changes immediately

            // Push Switch URL/path overrides to the shared configuration
            var s = Settings.Instance;
            SwitchConfiguration.TitleDBUrlOverride  = string.IsNullOrWhiteSpace(s.SwitchTitleDBUrlOverride)  ? null : s.SwitchTitleDBUrlOverride;
            SwitchConfiguration.VersionsUrlOverride = string.IsNullOrWhiteSpace(s.SwitchVersionsUrlOverride) ? null : s.SwitchVersionsUrlOverride;
            SwitchConfiguration.TitleDBPathOverride = string.IsNullOrWhiteSpace(s.SwitchTitleDBPathOverride) ? null : s.SwitchTitleDBPathOverride;

            Close();
        };

        var cancelBtn = new Button { Content = "Cancel" };
        cancelBtn.Click += (_, _) => Close();

        panel.Children.Add(cancelBtn);
        panel.Children.Add(saveBtn);
        return panel;
    }

    // ── Path / key status ─────────────────────────────────────────────────

    private static readonly IBrush OkBrush   = new SolidColorBrush(Color.FromRgb(0x2E, 0x9E, 0x44));
    private static readonly IBrush BadBrush  = new SolidColorBrush(Color.FromRgb(0xD3, 0x3A, 0x2F));

    private static void SetStatus(TextBlock tb, string text, IBrush brush)
    {
        tb.Text = text;
        tb.Foreground = brush;
    }

    /// <summary>
    /// Live "Found" / "Not found" / "Not set" indicator for a folder text box. Debounced, and the
    /// existence check runs off the UI thread so slow network paths don't block typing.
    /// </summary>
    private TextBlock MakePathStatus(TextBox box, string emptyText = "Not set")
    {
        var status = new TextBlock { FontSize = 11, MinWidth = 70, VerticalAlignment = VerticalAlignment.Center };
        var version = 0;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };

        async Task CheckAsync()
        {
            var text = box.Text?.Trim() ?? "";
            var my = ++version;
            if (text.Length == 0) { SetStatus(status, emptyText, Brushes.Gray); return; }

            var exists = await Task.Run(() =>
            {
                try { return Directory.Exists(ResolvePath(text)); }
                catch { return false; }
            });
            if (my == version) // ignore stale results
                SetStatus(status, exists ? "Found" : "Not found", exists ? OkBrush : BadBrush);
        }

        timer.Tick += (_, _) => { timer.Stop(); _ = CheckAsync(); };
        box.TextChanged += (_, _) => { timer.Stop(); timer.Start(); };
        Closed += (_, _) => timer.Stop();
        _ = CheckAsync();
        return status;
    }

    // Relative paths are resolved against the app base folder (as the rest of the app does).
    private static string ResolvePath(string text)
    {
        var expanded = Environment.ExpandEnvironmentVariables(text);
        if (System.IO.Path.IsPathRooted(expanded)) return expanded;
        var baseDir = string.IsNullOrEmpty(RomManagerConfiguration.BaseFolder)
            ? AppContext.BaseDirectory
            : RomManagerConfiguration.BaseFolder;
        return System.IO.Path.Combine(baseDir, expanded);
    }

    private static async Task RefreshKeyStatusAsync(StackPanel panel)
    {
        RomManagerShared.Switch.SwitchKeyStatus status;
        try
        {
            status = await Task.Run(RomManagerShared.Switch.SwitchKeyStatus.ReadDefault);
        }
        catch (Exception ex)
        {
            FileUtils.Log($"[SettingsWindow] Key status check failed: {ex.GetType().Name}");
            return;
        }

        panel.Children.Clear();

        var prod = status.ProdKeys;
        var prodLine = MakeLabel("");
        if (!prod.Exists)
            SetStatus(prodLine, "prod.keys: Not found (required for NSP/XCI)", BadBrush);
        else if (prod.Error is not null)
            SetStatus(prodLine, $"prod.keys: Found, could not be read ({prod.Error})", BadBrush);
        else
        {
            var master = status.HighestMasterKey is { } mk
                ? $", highest master_key_{mk:x2}" + (status.FirmwareHint is { } fw ? $" (firmware {fw}+)" : "")
                : ", no master keys";
            SetStatus(prodLine, $"prod.keys: Found, {prod.KeyCount} keys{master}",
                prod.KeyCount > 0 ? OkBrush : BadBrush);
        }
        panel.Children.Add(prodLine);

        var title = status.TitleKeys;
        var titleLine = MakeLabel("");
        if (!title.Exists)
            SetStatus(titleLine, "title.keys: Not found (optional)", Brushes.Gray);
        else if (title.Error is not null)
            SetStatus(titleLine, $"title.keys: Found, could not be read ({title.Error})", BadBrush);
        else
            SetStatus(titleLine, $"title.keys: Found, {title.KeyCount} title keys", OkBrush);
        panel.Children.Add(titleLine);
    }

    // ── UI helpers ────────────────────────────────────────────────────────

    private static TextBlock MakeLabel(string text, double fontSize = 12, bool bold = false, bool muted = false)
        => new()
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = bold ? FontWeight.SemiBold : FontWeight.Normal,
            Foreground = muted ? Brushes.Gray : null,
            TextWrapping = TextWrapping.Wrap
        };

    private static TextBox MakeTextBox(string text, double w = 200)
        => new() { Text = text, Width = w, HorizontalAlignment = HorizontalAlignment.Left };

    private static Separator MakeSeparator()
        => new() { Margin = new Thickness(0, 4, 0, 4) };

    private static Control MakePathRow(TextBox box, string buttonLabel, Func<Task> browseAction)
    {
        var btn = new Button { Content = buttonLabel, Margin = new Thickness(4, 0, 0, 0) };
        btn.Click += async (_, _) => await browseAction();

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children = { box, btn }
        };
    }

    /// <summary>
    /// Inline label + text box + browse button on a single row.
    /// The label is fixed-width so all inputs in the same panel align.
    /// </summary>
    private static Control MakeLabeledPathRow(
        string label, TextBox box, string buttonLabel, Func<Task> browseAction,
        Control? extraButton = null, Control? status = null)
    {
        var btn = new Button { Content = buttonLabel, Margin = new Thickness(4, 0, 0, 0) };
        btn.Click += async (_, _) => await browseAction();

        var lbl = new TextBlock
        {
            Text = label,
            Width = 140,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        };

        var sp = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
        };
        sp.Children.Add(lbl);
        sp.Children.Add(box);
        sp.Children.Add(btn);
        if (extraButton is not null)
            sp.Children.Add(extraButton);
        if (status is not null)
            sp.Children.Add(status);
        return sp;
    }
}

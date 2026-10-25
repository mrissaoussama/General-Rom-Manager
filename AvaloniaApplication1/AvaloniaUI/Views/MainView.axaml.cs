using Avalonia.Input;
using Avalonia.Media.Imaging;
using System.Net.Http;

namespace AvaloniaUI.Views;

public class MainView : UserControl
{
    private TabControl _consoleTabs = null!;
    private ColumnManager _columnManager;
    private Grid _mainGrid = null!;
    private StackPanel _detailsPanel = null!;
    private bool _initialized = false;
    private readonly Dictionary<string, DataGrid> _consoleDataGrids = new(StringComparer.OrdinalIgnoreCase);
    private Button _refreshCurrentBtn = null!;
    private TextBox _globalSearchBox = null!;
    private bool _isGrouped = false;

    // ── Bottom panel tabs ─────────────────────────────────────────────────
    private TabControl _bottomTabs = null!;
    private TextBox _logTextBox = null!;

    // ── Async image loader ────────────────────────────────────────────────
    private static readonly HttpClient _imgHttp;
    static MainView()
    {
        var handler = new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.All };
        _imgHttp = new HttpClient(handler);
        _imgHttp.DefaultRequestHeaders.UserAgent.ParseAdd("RomManager/1.0");
        _imgHttp.Timeout = TimeSpan.FromSeconds(15);
    }

    public MainView()
    {
        _columnManager = new ColumnManager();

        DataContextChanged += (sender, e) =>
        {
            if (DataContext is MainViewModel vm)
            {
                if (!_initialized)
                {
                    InitializeUI();
                    _initialized = true;
                }

                vm.ConsoleViewModels.CollectionChanged += (s, args) =>
                {
                    RefreshConsoleTabs();
                };

                RefreshConsoleTabs();
            }
        };
    }

    private void InitializeUI()
    {
        // ── Outer grid: top area (tabs+details) | splitter | bottom panel ──
        var outerGrid = new Grid();
        outerGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));           // row 0 – main content
        outerGrid.RowDefinitions.Add(new RowDefinition(new GridLength(5)));          // row 1 – horizontal splitter
        outerGrid.RowDefinitions.Add(new RowDefinition(new GridLength(200)));        // row 2 – bottom panel

        // ── Top area: console tabs | vertical splitter | details panel ────
        _mainGrid = new Grid();
        _mainGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        _mainGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(5)));
        _mainGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(280)));

        _consoleTabs = new TabControl { Name = "ConsoleTabs" };
        _consoleTabs.SelectionChanged += OnTabSelectionChanged;

        var sidePanel = CreateSidePanel();

        var vSplitter = new GridSplitter
        {
            ResizeDirection = GridResizeDirection.Columns,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Width = 5,
        };

        Grid.SetColumn(_consoleTabs, 0);
        _mainGrid.Children.Add(_consoleTabs);
        Grid.SetColumn(vSplitter, 1);
        _mainGrid.Children.Add(vSplitter);
        Grid.SetColumn(sidePanel, 2);
        _mainGrid.Children.Add(sidePanel);

        Grid.SetRow(_mainGrid, 0);
        outerGrid.Children.Add(_mainGrid);

        // ── Horizontal splitter (between main content and bottom panel) ───
        var hSplitter = new GridSplitter
        {
            ResizeDirection = GridResizeDirection.Rows,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Height = 5,
        };
        Grid.SetRow(hSplitter, 1);
        outerGrid.Children.Add(hSplitter);

        // ── Bottom panel: Log + Downloads tabs ────────────────────────────
        var bottomPanel = BuildBottomPanel();
        Grid.SetRow(bottomPanel, 2);
        outerGrid.Children.Add(bottomPanel);

        // ── Toolbar ───────────────────────────────────────────────────────
        var toolbar = BuildToolbar();

        var rootDock = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(toolbar, Dock.Top);
        rootDock.Children.Add(toolbar);

        var statusBar = BuildStatusBar();
        DockPanel.SetDock(statusBar, Dock.Bottom);
        rootDock.Children.Add(statusBar);

        rootDock.Children.Add(outerGrid);
        Content = rootDock;

        // ── Keyboard shortcuts ────────────────────────────────────────────
        KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.F5),
            Command = new RelayCommand(() => { if (DataContext is MainViewModel vm) vm.RefreshCommand.Execute(null); }),
        });
        KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.F, KeyModifiers.Control),
            Command = new RelayCommand(() => _globalSearchBox?.Focus()),
        });
        KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.OemComma, KeyModifiers.Control),
            Command = new RelayCommand(() => { if (DataContext is MainViewModel vm) vm.OpenSettingsCommand.Execute(null); }),
        });
        KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.L, KeyModifiers.Control),
            Command = new RelayCommand(() => { if (_bottomTabs is not null) _bottomTabs.SelectedIndex = 0; }),
        });

        // Subscribe to column visibility changes from Settings
        Settings.ColumnsChanged += consoleName =>
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (_consoleDataGrids.TryGetValue(consoleName, out var dg)
                    && DataContext is MainViewModel mainVM)
                {
                    var vm = mainVM.ConsoleViewModels
                        .FirstOrDefault(c => string.Equals(c.ConsoleName, consoleName, StringComparison.OrdinalIgnoreCase));
                    if (vm is not null)
                        BuildColumnsFromDescriptors(dg, vm);
                }
            });
    }

    // ── Grouped toolbar (File / View / Data dropdowns + search) ──────────

    private Border BuildToolbar()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 2,
            Margin = new Thickness(4, 2),
        };

        // ── File dropdown ──────────────────────────────────────────────────
        var fileBtn = new Button { Content = "File ▾", Padding = new Thickness(8, 4), FontSize = 12 };
        ToolTip.SetTip(fileBtn, "Settings and exit");
        fileBtn.Click += (_, _) =>
        {
            var flyout = new MenuFlyout();
            var settingsMi = new MenuItem { Header = "⚙ Settings…", InputGesture = new KeyGesture(Key.OemComma, KeyModifiers.Control) };
            settingsMi.Click += (_, _) => { if (DataContext is MainViewModel vm) vm.OpenSettingsCommand.Execute(null); };
            flyout.Items.Add(settingsMi);
            flyout.Items.Add(new Separator());
            var exitMi = new MenuItem { Header = "Exit" };
            exitMi.Click += (_, _) => { if (DataContext is MainViewModel vm) vm.ExitCommand.Execute(null); };
            flyout.Items.Add(exitMi);
            flyout.ShowAt(fileBtn);
        };
        panel.Children.Add(fileBtn);

        // ── View dropdown ──────────────────────────────────────────────────
        var viewBtn = new Button { Content = "View ▾", Padding = new Thickness(8, 4), FontSize = 12 };
        ToolTip.SetTip(viewBtn, "Columns, theme, compact mode");
        viewBtn.Click += (_, _) =>
        {
            var flyout = new MenuFlyout();
            var colsMi = new MenuItem { Header = "☑ Columns…" };
            colsMi.Click += (_, _) => ShowUnifiedColumnsMenu(viewBtn);
            flyout.Items.Add(colsMi);
            flyout.Items.Add(new Separator());
            var themeSub = new MenuItem { Header = "🎨 Theme" };
            foreach (var (label, key) in new[] { ("☀ Light", "Light"), ("🌙 Dark", "Dark"), ("⚙ System", "Default") })
            {
                var captured = key;
                var mi = new MenuItem { Header = label };
                mi.Click += (_, _) => { if (DataContext is MainViewModel vm) vm.SetThemeCommand.Execute(captured); };
                themeSub.Items.Add(mi);
            }
            flyout.Items.Add(themeSub);
            var compactMi = new MenuItem { Header = Settings.Instance.IsCompactMode ? "☑ Compact" : "☐ Compact" };
            compactMi.Click += (_, _) =>
            {
                if (DataContext is MainViewModel vm) { vm.ToggleCompactModeCommand.Execute(null); RefreshConsoleTabs(); }
            };
            flyout.Items.Add(compactMi);

            flyout.Items.Add(new Separator());
            var groupMi = new MenuItem { Header = _isGrouped ? "☑ Group by Title" : "☐ Group by Title" };
            groupMi.Click += (_, _) =>
            {
                _isGrouped = !_isGrouped;
                ApplyGrouping();
            };
            flyout.Items.Add(groupMi);

            flyout.ShowAt(viewBtn);
        };
        panel.Children.Add(viewBtn);

        // ── Data dropdown ──────────────────────────────────────────────────
        var dataBtn = new Button { Content = "Data ▾", Padding = new Thickness(8, 4), FontSize = 12 };
        ToolTip.SetTip(dataBtn, "Refresh, check updates");
        dataBtn.Click += (_, _) =>
        {
            var flyout = new MenuFlyout();
            var refreshAllMi = new MenuItem { Header = "⟳ Refresh All", InputGesture = new KeyGesture(Key.F5) };
            refreshAllMi.Click += (_, _) => { if (DataContext is MainViewModel vm) vm.RefreshCommand.Execute(null); };
            flyout.Items.Add(refreshAllMi);
            var consoleName = GetSelectedConsoleVM()?.ConsoleName ?? "Console";
            var refreshTabMi = new MenuItem { Header = $"⟳ Refresh {consoleName}" };
            refreshTabMi.Click += async (_, _) =>
            {
                var console = GetSelectedConsoleVM();
                if (console is not null) { await console.LoadData(); if (DataContext is MainViewModel mv) mv.RecalculateTotalRomCount(); }
            };
            flyout.Items.Add(refreshTabMi);
            flyout.Items.Add(new Separator());
            var checkUpdMi = new MenuItem { Header = "⬇ Check for Updates" };
            checkUpdMi.Click += (_, _) => { if (DataContext is MainViewModel vm) vm.CheckForUpdatesCommand.Execute(null); };
            flyout.Items.Add(checkUpdMi);
            flyout.ShowAt(dataBtn);
        };
        panel.Children.Add(dataBtn);

        panel.Children.Add(MakeToolbarSep());

        // ── Quick-access: Refresh current console ──────────────────────────
        _refreshCurrentBtn = new Button { Content = "⟳ Refresh", Padding = new Thickness(6, 3), FontSize = 11 };
        ToolTip.SetTip(_refreshCurrentBtn, "Reload the active console tab");
        _refreshCurrentBtn.Click += async (_, _) =>
        {
            var console = GetSelectedConsoleVM();
            if (console is not null) { await console.LoadData(); if (DataContext is MainViewModel mv) mv.RecalculateTotalRomCount(); }
        };
        panel.Children.Add(_refreshCurrentBtn);

        panel.Children.Add(MakeToolbarSep());

        // ── Global search box ─────────────────────────────────────────────
        _globalSearchBox = new TextBox
        {
            Watermark = "🔍 Filter…",
            Width = 200,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2, 0, 0, 0),
            FontSize = 11,
        };
        _globalSearchBox.TextChanged += (_, _) => ApplySearchFilter();
        panel.Children.Add(_globalSearchBox);

        var clearBtn = new Button { Content = "✕", Padding = new Thickness(4, 1), Margin = new Thickness(1, 0), FontSize = 10 };
        clearBtn.Click += (_, _) => _globalSearchBox.Text = string.Empty;
        panel.Children.Add(clearBtn);

        return new Border
        {
            Child = panel,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(0, 1),
        };
    }

    private static Control MakeToolbarSep() =>
        new Border { Width = 1, Height = 18, Margin = new Thickness(2, 0), Opacity = 0.3, Background = Brushes.Gray };

    // ═════════════════════════════════════════════════════════════════════
    // BOTTOM PANEL — Log + Downloads tabs (resizable via horizontal GridSplitter)
    // ═════════════════════════════════════════════════════════════════════

    private Control BuildBottomPanel()
    {
        _bottomTabs = new TabControl { TabStripPlacement = Dock.Top };

        // ── Log tab ───────────────────────────────────────────────────────
        _logTextBox = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = new Avalonia.Media.FontFamily("Cascadia Mono,Consolas,Monospace"),
            FontSize = 11,
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
        };
        LoadLogText();

        // Live log: subscribe to FileUtils.LogAdded so new entries appear instantly
        FileUtils.LogAdded += entry =>
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (_logTextBox is null) return;
                var current = _logTextBox.Text ?? "";
                _logTextBox.Text = current.Length > 0
                    ? current + Environment.NewLine + entry
                    : entry;
                _logTextBox.CaretIndex = _logTextBox.Text.Length;
            });

        var logToolbar = new DockPanel { Margin = new Thickness(4, 2) };
        var refreshLogBtn = new Button { Content = "↻", Padding = new Thickness(5, 1), FontSize = 11 };
        ToolTip.SetTip(refreshLogBtn, "Refresh log");
        refreshLogBtn.Click += (_, _) => LoadLogText();
        DockPanel.SetDock(refreshLogBtn, Dock.Right);
        logToolbar.Children.Add(refreshLogBtn);

        var openLogBtn = new Button { Content = "Open File…", Padding = new Thickness(5, 1), FontSize = 11, Margin = new Thickness(0, 0, 4, 0) };
        openLogBtn.Click += (_, _) =>
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(FileUtils.LogFilePath) { UseShellExecute = true }); }
            catch (Exception ex) { FileUtils.Log($"Cannot open log file: {ex.Message}"); }
        };
        DockPanel.SetDock(openLogBtn, Dock.Right);
        logToolbar.Children.Add(openLogBtn);
        logToolbar.Children.Add(new TextBlock { Text = "Application Log", FontWeight = FontWeight.SemiBold, FontSize = 11, VerticalAlignment = VerticalAlignment.Center });

        var logPanel = new DockPanel();
        DockPanel.SetDock(logToolbar, Dock.Top);
        logPanel.Children.Add(logToolbar);
        logPanel.Children.Add(new ScrollViewer
        {
            Content = _logTextBox,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        });

        var logTab = new TabItem { Header = "🗒 Log", Content = logPanel, Padding = new Thickness(8, 2), FontSize = 11 };

        // ── Downloads tab ─────────────────────────────────────────────────
        var dlTab = new TabItem { Header = "📥 Downloads", Content = BuildDownloadsPanel(), Padding = new Thickness(8, 2), FontSize = 11 };

        // ── Operations tab (live OperationQueue view) ────────────────────
        var opsTab = new TabItem
        {
            Header = "⚙ Operations",
            Content = BuildOperationsPanel(),
            Padding = new Thickness(8, 2),
            FontSize = 11,
        };

        _bottomTabs.Items.Add(logTab);
        _bottomTabs.Items.Add(dlTab);
        _bottomTabs.Items.Add(opsTab);
        return _bottomTabs;
    }

    private void LoadLogText()
    {
        if (_logTextBox is null) return;
        var lines = FileUtils.GetRecentLogs();
        _logTextBox.Text = lines.Count > 0
            ? string.Join(Environment.NewLine, lines)
            : "(No log entries yet.)";
        _logTextBox.CaretIndex = _logTextBox.Text?.Length ?? 0;
    }

    private Control BuildDownloadsPanel()
    {
        var root = new DockPanel { LastChildFill = true };

        var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Margin = new Thickness(4, 2) };

        var pauseAllBtn = new Button { Content = "⏸ Pause", Padding = new Thickness(6, 1), FontSize = 11 };
        pauseAllBtn.Click += (_, _) => { if (DataContext is MainViewModel vm) vm.DownloadManager.PauseAllCommand.Execute(null); };
        toolbar.Children.Add(pauseAllBtn);

        var resumeAllBtn = new Button { Content = "▶ Resume", Padding = new Thickness(6, 1), FontSize = 11 };
        resumeAllBtn.Click += (_, _) => { if (DataContext is MainViewModel vm) vm.DownloadManager.ResumeAllCommand.Execute(null); };
        toolbar.Children.Add(resumeAllBtn);

        var clearBtn = new Button { Content = "✕ Clear", Padding = new Thickness(6, 1), FontSize = 11 };
        clearBtn.Click += (_, _) => { if (DataContext is MainViewModel vm) vm.DownloadManager.ClearCompletedCommand.Execute(null); };
        toolbar.Children.Add(clearBtn);

        DockPanel.SetDock(toolbar, Dock.Top);
        root.Children.Add(toolbar);

        // Download entries DataGrid with error column
        var dlGrid = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
            SelectionMode = DataGridSelectionMode.Single,
            RowHeight = 28,
            FontSize = 11,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };

        dlGrid.Columns.Add(new DataGridTextColumn { Header = "File", Binding = new Binding("FileName"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        dlGrid.Columns.Add(new DataGridTextColumn { Header = "Status", Binding = new Binding("Status"), Width = new DataGridLength(80) });
        dlGrid.Columns.Add(new DataGridTextColumn { Header = "Progress", Binding = new Binding("ProgressText"), Width = new DataGridLength(100) });
        dlGrid.Columns.Add(new DataGridTextColumn { Header = "Speed", Binding = new Binding("SpeedText"), Width = new DataGridLength(80) });
        dlGrid.Columns.Add(new DataGridTextColumn { Header = "Path", Binding = new Binding("DestinationPath"), Width = new DataGridLength(160) });
        dlGrid.Columns.Add(new DataGridTextColumn { Header = "Error", Binding = new Binding("ErrorMessage"), Width = new DataGridLength(200) });

        // Bind to DownloadManager.Downloads when DataContext becomes available
        // Use Bind() to ensure the DataGrid stays in sync with collection changes
        void BindDownloads()
        {
            if (DataContext is MainViewModel vm)
            {
                dlGrid.ItemsSource = vm.DownloadManager.Downloads;
                // Force refresh when the collection changes
                vm.DownloadManager.Downloads.CollectionChanged += (_, _) =>
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        var src = dlGrid.ItemsSource;
                        dlGrid.ItemsSource = null;
                        dlGrid.ItemsSource = src;
                    });
            }
        }
        if (DataContext is MainViewModel) BindDownloads();
        DataContextChanged += (_, _) => BindDownloads();

        root.Children.Add(dlGrid);
        return root;
    }

    // ── Operations panel (live OperationQueue view) ────────────────────────

    private StackPanel _opsListPanel = null!;
    private TextBlock _opsPlaceholder = null!;

    private Control BuildOperationsPanel()
    {
        var root = new DockPanel { LastChildFill = true };

        var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Margin = new Thickness(4, 2) };
        var clearBtn = new Button { Content = "✕ Clear Finished", Padding = new Thickness(6, 1), FontSize = 11 };
        clearBtn.Click += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.Operations.ClearFinished();
                RefreshOperationsPanel();
            }
        };
        toolbar.Children.Add(clearBtn);

        var cancelAllBtn = new Button { Content = "⏹ Cancel All", Padding = new Thickness(6, 1), FontSize = 11 };
        cancelAllBtn.Click += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.Operations.CancelAll();
                RefreshOperationsPanel();
            }
        };
        toolbar.Children.Add(cancelAllBtn);

        DockPanel.SetDock(toolbar, Dock.Top);
        root.Children.Add(toolbar);

        _opsPlaceholder = new TextBlock { Text = "No operations queued.", Opacity = 0.5, Margin = new Thickness(8), FontSize = 11 };
        _opsListPanel = new StackPanel { Margin = new Thickness(4), Spacing = 2 };
        _opsListPanel.Children.Add(_opsPlaceholder);

        var scroller = new ScrollViewer
        {
            Content = _opsListPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };
        root.Children.Add(scroller);

        // Subscribe to OperationQueue state changes when DataContext is available
        OperationQueue? _subscribedQueue = null;
        void BindOps()
        {
            if (DataContext is MainViewModel vm && vm.Operations != _subscribedQueue)
            {
                // Unsubscribe from old queue if any
                if (_subscribedQueue is not null)
                    _subscribedQueue.OperationStateChanged -= OnOpChanged;

                _subscribedQueue = vm.Operations;
                _subscribedQueue.OperationStateChanged += OnOpChanged;
                Avalonia.Threading.Dispatcher.UIThread.Post(RefreshOperationsPanel);
            }
        }
        void OnOpChanged(QueuedOperation _) =>
            Avalonia.Threading.Dispatcher.UIThread.Post(RefreshOperationsPanel);

        if (DataContext is MainViewModel) BindOps();
        DataContextChanged += (_, _) => BindOps();

        return root;
    }

    private void RefreshOperationsPanel()
    {
        if (_opsListPanel is null) return;
        if (DataContext is not MainViewModel vm) return;

        var ops = vm.Operations.Operations;
        _opsListPanel.Children.Clear();

        if (ops.Count == 0)
        {
            _opsListPanel.Children.Add(_opsPlaceholder);
            return;
        }

        foreach (var op in ops)
        {
            var row = new DockPanel { Margin = new Thickness(0, 1) };

            // Status icon
            var stateIcon = op.State switch
            {
                OperationState.Pending   => "⏳",
                OperationState.Running   => "▶",
                OperationState.Completed => "✅",
                OperationState.Failed    => "❌",
                OperationState.Cancelled => "⛔",
                OperationState.Undone    => "↩",
                _ => "?"
            };

            var iconLbl = new TextBlock { Text = stateIcon, Width = 24, FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
            DockPanel.SetDock(iconLbl, Dock.Left);
            row.Children.Add(iconLbl);

            // Cancel button (for pending/running)
            if (op.CanCancel)
            {
                var cancelBtn = new Button { Content = "✕", Padding = new Thickness(3, 0), FontSize = 10, Margin = new Thickness(0, 0, 4, 0) };
                var capturedOp = op;
                cancelBtn.Click += (_, _) => { vm.Operations.Cancel(capturedOp); RefreshOperationsPanel(); };
                DockPanel.SetDock(cancelBtn, Dock.Right);
                row.Children.Add(cancelBtn);
            }

            // Undo button (for completed undoable operations)
            if (op.CanUndo)
            {
                var undoBtn = new Button { Content = "↩ Undo", Padding = new Thickness(4, 0), FontSize = 10, Margin = new Thickness(0, 0, 4, 0) };
                var capturedOp = op;
                undoBtn.Click += async (_, _) => { await vm.Operations.UndoAsync(capturedOp); RefreshOperationsPanel(); };
                DockPanel.SetDock(undoBtn, Dock.Right);
                row.Children.Add(undoBtn);
            }

            // Progress
            if (op.State == OperationState.Running)
            {
                var progressBar = new ProgressBar { Value = op.ProgressPercent, Maximum = 100, Width = 80, Height = 8, Margin = new Thickness(4, 0) };
                DockPanel.SetDock(progressBar, Dock.Right);
                row.Children.Add(progressBar);
            }

            // Name + category
            var text = $"[{op.Category}] {op.Name}";
            if (!string.IsNullOrEmpty(op.ErrorMessage)) text += $" — {op.ErrorMessage}";
            var nameLbl = new TextBlock { Text = text, FontSize = 11, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
            row.Children.Add(nameLbl);

            _opsListPanel.Children.Add(row);
        }
    }

    // ── Status bar ────────────────────────────────────────────────────────

    private Border BuildStatusBar()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Margin = new Thickness(8, 2),
        };

        var romCountLabel = new TextBlock { VerticalAlignment = VerticalAlignment.Center, FontSize = 12 };
        romCountLabel.Bind(TextBlock.TextProperty,
            new Binding("StatusMessage") { Source = DataContext, Mode = BindingMode.OneWay });
        panel.Children.Add(romCountLabel);

        panel.Children.Add(new Separator { Width = 1, Height = 14 });

        var loadingText = new TextBlock { Text = "Loading…", VerticalAlignment = VerticalAlignment.Center, FontSize = 12, IsVisible = false };
        var loadingBar  = new ProgressBar { IsIndeterminate = true, Width = 80, Height = 10, IsVisible = false };

        void UpdateLoadingVisibility()
        {
            if (DataContext is not MainViewModel mainVm) return;
            var anyLoading = mainVm.ConsoleViewModels.Any(c => c.IsLoading);
            loadingText.IsVisible = anyLoading;
            loadingBar.IsVisible  = anyLoading;
        }

        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainViewModel vm2)
            {
                romCountLabel.Bind(TextBlock.TextProperty,
                    new Binding("StatusMessage") { Source = vm2, Mode = BindingMode.OneWay });

                void SubscribeToConsole(IConsoleViewModel cvm)
                {
                    if (cvm is System.ComponentModel.INotifyPropertyChanged npc)
                        npc.PropertyChanged += (__, pe) =>
                        {
                            if (pe.PropertyName == nameof(IConsoleViewModel.IsLoading))
                                Avalonia.Threading.Dispatcher.UIThread.Post(UpdateLoadingVisibility);
                        };
                }

                foreach (var c in vm2.ConsoleViewModels)
                    SubscribeToConsole(c);

                vm2.ConsoleViewModels.CollectionChanged += (_, args) =>
                {
                    if (args.NewItems != null)
                        foreach (IConsoleViewModel c in args.NewItems)
                            SubscribeToConsole(c);
                    UpdateLoadingVisibility();
                };
            }
        };

        panel.Children.Add(loadingBar);
        panel.Children.Add(loadingText);

        return new Border
        {
            Child = panel,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(0, 2),
        };
    }

    // ── Tab management ────────────────────────────────────────────────────

    /// <summary>
    /// Groups ROMs hierarchically by base Title. Each group has a "parent" Game row
    /// followed by indented Update/DLC children. When grouping is off, restores the
    /// live ObservableCollection binding.
    /// </summary>
    private void ApplyGrouping()
    {
        var console = GetSelectedConsoleVM();
        if (console is null) return;
        if (!_consoleDataGrids.TryGetValue(console.ConsoleName, out var dg)) return;

        if (!_isGrouped)
        {
            // Restore original order
            dg.ItemsSource = null;
            dg.Bind(DataGrid.ItemsSourceProperty, new Binding("Roms") { Source = console });
            return;
        }

        // Build hierarchical groups:
        //   Key = base title (cleaned), Value = [Game first, then Updates sorted by version, then DLCs]
        var groups = console.Roms
            .GroupBy(r => NormalizeTitleForGrouping(r))
            .OrderBy(g => g.Key)
            .ToList();

        var result = new List<RomViewModel>();
        foreach (var group in groups)
        {
            var items = group.OrderBy(r => r.RomType switch
            {
                "Game"     => 0,
                "Update"   => 1,
                "DLC"      => 2,
                "Homebrew" => 3,
                _          => 4,
            }).ThenBy(r => r.Version).ToList();

            if (items.Count == 1)
            {
                // Single item — no hierarchy needed
                result.Add(items[0]);
            }
            else
            {
                // First item is the parent (ideally a Game)
                var parent = items[0];
                parent.GroupDepth = 0;
                parent.IsGroupHeader = true;
                parent.ChildCount = items.Count - 1;
                result.Add(parent);

                // Remaining are children
                for (int i = 1; i < items.Count; i++)
                {
                    items[i].GroupDepth = 1;
                    items[i].IsGroupHeader = false;
                    result.Add(items[i]);
                }
            }
        }

        dg.ItemsSource = result;
    }

    /// <summary>Extract a normalized group key from a RomViewModel for title-based grouping.</summary>
    private static string NormalizeTitleForGrouping(RomViewModel rvm)
    {
        // Use TitleID prefix (first 13 chars for Switch titles share base) or Title text
        var titleId = rvm.TitleID;
        if (!string.IsNullOrEmpty(titleId) && titleId.Length >= 13)
        {
            // Switch: base title ID = first 13 chars + "000"
            return titleId[..13];
        }
        // Fall back to title text
        return rvm.Title?.Trim() ?? titleId ?? "Unknown";
    }

    private void RefreshConsoleTabs()
    {
        if (DataContext is MainViewModel vm && _consoleTabs != null)
        {
            _consoleTabs.Items.Clear();

            foreach (var console in vm.ConsoleViewModels)
            {
                var tabItem = new TabItem
                {
                    Header = console.ConsoleName,
                    Content = CreateTabContent(console),
                    DataContext = console,
                    // ── Compact tabs ──────────────────────────────────
                    Padding = new Thickness(8, 2),
                    FontSize = 11,
                };
                _consoleTabs.Items.Add(tabItem);
            }

            if (_consoleTabs.Items.Count > 0)
                _consoleTabs.SelectedIndex = 0;
        }
    }

    private Control CreateTabContent(IConsoleViewModel console)
    {
        var dockPanel = new DockPanel();
        bool compact = Settings.Instance.IsCompactMode;

        // ── Requirement warning banner ────────────────────────────────────
        var unmetReqs = console.GetUnmetRequirements();
        if (unmetReqs.Count > 0)
        {
            var reqNames = string.Join(", ", unmetReqs.Select(r => r.DisplayName));
            var warningBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 180, 0)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4),
                Margin = new Thickness(5, 2, 5, 4),
                Child = new TextBlock
                {
                    Text = $"⚠ Missing: {reqNames}. Some features may be unavailable. Check Settings.",
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Color.Parse("#B87A00")),
                },
            };
            DockPanel.SetDock(warningBorder, Dock.Top);
            dockPanel.Children.Add(warningBorder);
        }

        // Status panel (progress bar + status text) — docked at top
        var statusPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(5, 4, 5, 2),
        };

        var progressBar = new ProgressBar { Maximum = 100, Width = 100, Height = 8 };
        progressBar.Bind(ProgressBar.IsVisibleProperty, new Binding("IsLoading") { Source = console });
        progressBar.Bind(ProgressBar.IsIndeterminateProperty, new Binding("IsIndeterminate") { Source = console });
        progressBar.Bind(ProgressBar.ValueProperty, new Binding("ProgressPercent") { Source = console });

        var statusText = new TextBox
        {
            IsReadOnly = true,
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Padding = new Thickness(0),
            TextWrapping = TextWrapping.NoWrap,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 11,
        };
        statusText.Bind(TextBox.TextProperty, new Binding("StatusMessage") { Source = console });

        statusPanel.Children.Add(progressBar);
        statusPanel.Children.Add(statusText);
        DockPanel.SetDock(statusPanel, Dock.Top);
        dockPanel.Children.Add(statusPanel);

        // DataGrid
        var dataGrid = new DataGrid
        {
            Name = "RomsDataGrid",
            AutoGenerateColumns = false,
            IsReadOnly = true,
            GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
            SelectionMode = DataGridSelectionMode.Extended,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = compact ? new Thickness(2) : new Thickness(4),
            MinHeight = 100,
            RowHeight = compact ? 22 : double.NaN,
            FontSize = compact ? 11 : 12,
        };

        dataGrid.Bind(DataGrid.ItemsSourceProperty, new Binding("Roms") { Source = console });

        dataGrid.SelectionChanged += (_, _) =>
        {
            if (dataGrid.SelectedItem is RomViewModel rvm)
                ShowRomDetails(rvm);
            else
                ClearRomDetails();
        };

        // Visual indentation for grouped rows (tree-like hierarchy)
        dataGrid.LoadingRow += (_, e) =>
        {
            if (!_isGrouped) return;
            if (e.Row.DataContext is RomViewModel rvm)
            {
                if (rvm.IsGroupHeader && rvm.ChildCount > 0)
                {
                    // Group header — bold + child count indicator
                    e.Row.FontWeight = FontWeight.SemiBold;
                    e.Row.Background = new SolidColorBrush(Color.FromArgb(15, 128, 128, 128));
                }
                else if (rvm.GroupDepth > 0)
                {
                    // Child row — indented
                    e.Row.FontWeight = FontWeight.Normal;
                    e.Row.Margin = new Thickness(20, 0, 0, 0);
                    e.Row.Opacity = 0.85;
                }
                else
                {
                    e.Row.FontWeight = FontWeight.Normal;
                    e.Row.Margin = new Thickness(0);
                    e.Row.Opacity = 1.0;
                }
            }
        };

        dataGrid.ContextMenu = BuildContextMenu(console, dataGrid);

        BuildColumnsFromDescriptors(dataGrid, console);
        _consoleDataGrids[console.ConsoleName] = dataGrid;

        dockPanel.Children.Add(dataGrid);
        return dockPanel;
    }

    private void ApplySearchFilter()
    {
        var console = GetSelectedConsoleVM();
        if (console is null) return;
        if (!_consoleDataGrids.TryGetValue(console.ConsoleName, out var dg)) return;

        var q = _globalSearchBox?.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(q))
        {
            // Restore the live binding so ObservableCollection updates work again
            dg.ItemsSource = null;
            dg.Bind(DataGrid.ItemsSourceProperty, new Binding("Roms") { Source = console });
        }
        else
        {
            // Static filtered snapshot — user must clear search to get live updates
            var filtered = console.Roms
                .Where(r => (r.Title?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                         || (r.TitleID?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                         || (r.Path?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                         || (r.RomType?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
            dg.ItemsSource = filtered;
        }
    }

    private IConsoleViewModel? GetSelectedConsoleVM()
    {
        if (_consoleTabs?.SelectedItem is TabItem { DataContext: IConsoleViewModel vm })
            return vm;
        return null;
    }

    // ── Column management ─────────────────────────────────────────────────

    private static void BuildColumnsFromDescriptors(DataGrid dataGrid, IConsoleViewModel console)
    {
        var settings = Settings.Instance;
        if (!settings.VisibleColumns.ContainsKey(console.ConsoleName))
        {
            settings.VisibleColumns[console.ConsoleName] = ["TitleID", "Title", "RomType", "Region", "Version"];
            settings.Save();
        }
        var visible = settings.VisibleColumns[console.ConsoleName];

        dataGrid.Columns.Clear();
        foreach (var (propName, header, usesDict) in console.GetColumnDescriptors())
        {
            var binding = usesDict
                ? new Binding("PropertiesDictionary")
                  { Converter = new DictionaryItemConverter(), ConverterParameter = propName }
                : new Binding(propName);

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header    = header,
                Binding   = binding,
                IsVisible = visible.Contains(propName),
                Tag       = usesDict ? $"Specific:{propName}" : $"Base:{propName}",
            });
        }
    }

    private void ShowUnifiedColumnsMenu(Control anchor)
    {
        if (DataContext is not MainViewModel mainVM) return;

        var settings = Settings.Instance;
        var consoles = mainVM.ConsoleViewModels.ToList();

        var sp = new StackPanel { Spacing = 10, Margin = new Thickness(12), MinWidth = 400 };

        var currentConsoleName = (_consoleTabs.SelectedItem as TabItem)?.Header?.ToString() ?? "";
        var ordered = consoles
            .OrderBy(c => c.ConsoleName == currentConsoleName ? 0 : 1)
            .ThenBy(c => c.ConsoleName)
            .ToList();

        if (!ordered.Any())
        {
            sp.Children.Add(new TextBlock
            {
                Text = "No consoles loaded yet. Add ROM paths in Settings first.",
                TextWrapping = TextWrapping.Wrap,
            });
        }
        else
        {
            foreach (var console in ordered)
            {
                var capConsole = console;

                var headerRow = new DockPanel();
                var headerLbl = new TextBlock
                {
                    Text = console.ConsoleName,
                    FontWeight = FontWeight.SemiBold,
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                DockPanel.SetDock(headerLbl, Dock.Left);
                headerRow.Children.Add(headerLbl);

                var resetLink = new Button
                {
                    Content = "Reset",
                    Padding = new Thickness(6, 1),
                    Margin = new Thickness(8, 0, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    FontSize = 11,
                };
                resetLink.Click += (_, _) =>
                {
                    settings.VisibleColumns[capConsole.ConsoleName] = ["TitleID", "Title", "RomType", "Region", "Version"];
                    Settings.RaiseColumnsChanged(capConsole.ConsoleName);
                    settings.Save();
                };
                DockPanel.SetDock(resetLink, Dock.Right);
                headerRow.Children.Add(resetLink);
                sp.Children.Add(headerRow);

                if (!settings.VisibleColumns.ContainsKey(console.ConsoleName))
                    settings.VisibleColumns[console.ConsoleName] = ["TitleID", "Title", "RomType", "Region", "Version"];

                var wrap = new WrapPanel { Orientation = Orientation.Horizontal, MaxWidth = 460 };
                foreach (var (propName, colHeader, _) in console.GetColumnDescriptors())
                {
                    var capProp = propName;
                    var cb = new CheckBox
                    {
                        Content = colHeader,
                        IsChecked = settings.VisibleColumns[console.ConsoleName].Contains(capProp),
                        Margin = new Thickness(0, 0, 10, 3),
                    };
                    cb.IsCheckedChanged += (_, _) =>
                    {
                        var cols = settings.VisibleColumns[capConsole.ConsoleName];
                        if (cb.IsChecked == true && !cols.Contains(capProp))  cols.Add(capProp);
                        else if (cb.IsChecked == false)                         cols.Remove(capProp);
                        Settings.RaiseColumnsChanged(capConsole.ConsoleName);
                    };
                    wrap.Children.Add(cb);
                }
                sp.Children.Add(wrap);
                sp.Children.Add(new Separator { Margin = new Thickness(0, 4, 0, 0) });
            }
        }

        var flyout = new Flyout
        {
            Placement = PlacementMode.BottomEdgeAlignedLeft,
            Content = new ScrollViewer
            {
                Content = sp,
                MaxHeight = 460,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            },
        };
        flyout.ShowAt(anchor);
    }

    private void OnTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_consoleTabs?.SelectedItem is TabItem tabItem)
        {
            var consoleName = tabItem.Header?.ToString() ?? "";
            if (_refreshCurrentBtn is not null)
                _refreshCurrentBtn.Content = $"⟳ {consoleName}";

            if (tabItem.Content is DockPanel dockPanel)
            {
                var dataGrid = dockPanel.Children.OfType<DataGrid>().FirstOrDefault();
                if (dataGrid != null)
                {
                    ColumnManager.currentDataGrid = dataGrid;
                    if (tabItem.DataContext is IConsoleViewModel vm)
                        BuildColumnsFromDescriptors(dataGrid, vm);
                    else if (DataContext is MainViewModel mainVM)
                    {
                        var consoleVM = mainVM.ConsoleViewModels.FirstOrDefault(c => c.ConsoleName == consoleName);
                        if (consoleVM != null) BuildColumnsFromDescriptors(dataGrid, consoleVM);
                    }
                }
            }

            // Re-apply search filter for new tab
            ApplySearchFilter();
        }
    }

    // ── Details side panel ────────────────────────────────────────────────

    private Control CreateSidePanel()
    {
        var sidePanel = new DockPanel();

        var header = new TextBlock
        {
            Text = "ROM Details",
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(5)
        };
        DockPanel.SetDock(header, Dock.Top);
        sidePanel.Children.Add(header);

        var placeholder = new TextBlock
        {
            Text = "Select a ROM to see details.",
            Opacity = 0.6,
            FontSize = 12,
            Margin = new Thickness(6),
            TextWrapping = TextWrapping.Wrap,
        };

        _detailsPanel = new StackPanel { Margin = new Thickness(5) };
        _detailsPanel.Children.Add(placeholder);

        var detailsScroller = new ScrollViewer
        {
            Content = _detailsPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        sidePanel.Children.Add(detailsScroller);

        return sidePanel;
    }

    private void ShowRomDetails(RomViewModel rvm)
    {
        if (_detailsPanel is null) return;
        _detailsPanel.Children.Clear();

        // ── ROM images (Icon / Banner / Thumbnail) ────────────────────────
        var rom = rvm.RomSource;
        var imageUrls = new List<string>();
        if (!string.IsNullOrEmpty(rom.Icon)) imageUrls.Add(rom.Icon);
        if (!string.IsNullOrEmpty(rom.Banner)) imageUrls.Add(rom.Banner);
        if (!string.IsNullOrEmpty(rom.Thumbnail)) imageUrls.Add(rom.Thumbnail);
        if (rom.Images?.Count > 0) imageUrls.AddRange(rom.Images.Where(i => !string.IsNullOrEmpty(i)));

        if (imageUrls.Count > 0)
        {
            var imagePanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8),
            };
            foreach (var imgSource in imageUrls.Take(6)) // show up to 6
            {
                if (File.Exists(imgSource))
                {
                    try
                    {
                        var img = new Avalonia.Controls.Image
                        {
                            Source = new Bitmap(imgSource),
                            MaxWidth = 120,
                            MaxHeight = 120,
                            Margin = new Thickness(0, 0, 6, 6),
                            Stretch = Avalonia.Media.Stretch.Uniform,
                        };
                        imagePanel.Children.Add(img);
                    }
                    catch (Exception ex) { FileUtils.Log($"[Image] Failed to load local '{imgSource}': {ex.Message}"); }
                }
                else if (imgSource.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    // URL — async HTTP image loading
                    var imgControl = new Avalonia.Controls.Image
                    {
                        MaxWidth = 120,
                        MaxHeight = 120,
                        Width = 100,
                        Height = 70,
                        Margin = new Thickness(0, 0, 6, 6),
                        Stretch = Avalonia.Media.Stretch.Uniform,
                    };
                    ToolTip.SetTip(imgControl, imgSource);
                    imagePanel.Children.Add(imgControl);
                    _ = LoadImageFromUrlAsync(imgControl, imgSource);
                }
            }
            if (imagePanel.Children.Count > 0)
                _detailsPanel.Children.Add(imagePanel);
        }

        // Title
        _detailsPanel.Children.Add(new TextBlock
        {
            Text = rvm.Title,
            FontWeight = FontWeight.Bold,
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 6),
        });

        // Type badge
        _detailsPanel.Children.Add(new TextBlock
        {
            Text = $"{rvm.RomType}  ·  {rvm.Region}",
            FontSize = 11,
            Opacity = 0.6,
            Margin = new Thickness(0, 0, 0, 8),
        });

        // ── Rich properties — skip keys we'll render specially ────────────
        var specialKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Title", "Type", "Region", "Description", "Hashes", "Rating" };

        foreach (var kv in rvm.Properties)
        {
            if (string.IsNullOrWhiteSpace(kv.Value) || kv.Value == "N/A") continue;
            if (specialKeys.Contains(kv.Key)) continue;

            var row = new DockPanel { Margin = new Thickness(0, 1, 0, 1) };

            var key = new TextBlock
            {
                Text = kv.Key + ":",
                FontWeight = FontWeight.SemiBold,
                Width = 110,
                FontSize = 11,
                TextTrimming = TextTrimming.CharacterEllipsis,
            };
            DockPanel.SetDock(key, Dock.Left);
            row.Children.Add(key);

            var val = new TextBlock
            {
                Text = kv.Value,
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
            };
            row.Children.Add(val);

            _detailsPanel.Children.Add(row);
        }

        // ── Ratings (full detail with content descriptors) ────────────────
        if (rom.Ratings?.Any() == true)
        {
            _detailsPanel.Children.Add(new TextBlock
            {
                Text = "Ratings",
                FontWeight = FontWeight.SemiBold,
                FontSize = 12,
                Margin = new Thickness(0, 8, 0, 4),
            });
            foreach (var rating in rom.Ratings)
            {
                var ratingText = $"{rating.Name}: Age {rating.Age}+";
                if (rating.RatingContents?.Any() == true)
                    ratingText += $" ({string.Join(", ", rating.RatingContents)})";
                _detailsPanel.Children.Add(new TextBlock
                {
                    Text = ratingText,
                    FontSize = 11,
                    Margin = new Thickness(8, 0, 0, 2),
                    TextWrapping = TextWrapping.Wrap,
                });
            }
        }

        // ── Hashes (detailed table) ──────────────────────────────────────
        if (rom.Hashes?.Any() == true)
        {
            _detailsPanel.Children.Add(new TextBlock
            {
                Text = "Hashes",
                FontWeight = FontWeight.SemiBold,
                FontSize = 12,
                Margin = new Thickness(0, 8, 0, 4),
            });
            foreach (var hash in rom.Hashes)
            {
                var hashRow = new DockPanel { Margin = new Thickness(8, 0, 0, 2) };
                var hashType = new TextBlock
                {
                    Text = hash.Type.ToString() + ":",
                    FontWeight = FontWeight.SemiBold,
                    Width = 60,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.Parse("#6699CC")),
                };
                DockPanel.SetDock(hashType, Dock.Left);
                hashRow.Children.Add(hashType);

                var hashValue = new SelectableTextBlock
                {
                    Text = hash.Value,
                    FontSize = 10,
                    FontFamily = new Avalonia.Media.FontFamily("Cascadia Mono,Consolas,Monospace"),
                    TextWrapping = TextWrapping.Wrap,
                };
                hashRow.Children.Add(hashValue);

                _detailsPanel.Children.Add(hashRow);
            }
        }

        // ── Description (full text block) ─────────────────────────────────
        if (rom.Descriptions?.Any() == true)
        {
            var desc = rom.Descriptions.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.Value));
            if (desc != null)
            {
                _detailsPanel.Children.Add(new TextBlock
                {
                    Text = "Description",
                    FontWeight = FontWeight.SemiBold,
                    FontSize = 12,
                    Margin = new Thickness(0, 8, 0, 4),
                });
                _detailsPanel.Children.Add(new TextBlock
                {
                    Text = desc.Value,
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap,
                    Opacity = 0.85,
                    Margin = new Thickness(0, 0, 0, 6),
                    MaxHeight = 200,
                });
            }
        }

        // ── Related ROMs (for Game: Updates, DLCs, RomHacks) ─────────────
        if (rom is Game game)
        {
            AddRelatedRomSection("Updates", game.Updates);
            AddRelatedRomSection("DLCs", game.DLCs);
            AddRelatedRomSection("ROM Hacks", game.RomHacks);
        }
        else if (rom is Update upd && upd.RelatedGame != null)
        {
            _detailsPanel.Children.Add(new TextBlock
            {
                Text = $"Base Game: {upd.RelatedGame.Titles?.FirstOrDefault()?.Value ?? upd.RelatedGame.TitleID ?? "Unknown"}",
                FontSize = 11, Opacity = 0.7, Margin = new Thickness(0, 6, 0, 0),
            });
        }
        else if (rom is DLC dlc && dlc.RelatedGame != null)
        {
            _detailsPanel.Children.Add(new TextBlock
            {
                Text = $"Base Game: {dlc.RelatedGame.Titles?.FirstOrDefault()?.Value ?? dlc.RelatedGame.TitleID ?? "Unknown"}",
                FontSize = 11, Opacity = 0.7, Margin = new Thickness(0, 6, 0, 0),
            });
        }
    }

    private void AddRelatedRomSection(string label, IList<Game>? items)
    {
        if (items is null || items.Count == 0) return;
        _detailsPanel.Children.Add(new TextBlock
        {
            Text = $"{label} ({items.Count})",
            FontWeight = FontWeight.SemiBold,
            FontSize = 12,
            Margin = new Thickness(0, 8, 0, 4),
        });
        foreach (var item in items.Take(20))
        {
            var title = item.Titles?.FirstOrDefault()?.Value ?? item.TitleID ?? "Unknown";
            _detailsPanel.Children.Add(new TextBlock
            {
                Text = $"  • {title}",
                FontSize = 11,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(4, 0, 0, 1),
            });
        }
    }

    private void AddRelatedRomSection(string label, IList<Update>? items)
    {
        if (items is null || items.Count == 0) return;
        _detailsPanel.Children.Add(new TextBlock
        {
            Text = $"{label} ({items.Count})",
            FontWeight = FontWeight.SemiBold,
            FontSize = 12,
            Margin = new Thickness(0, 8, 0, 4),
        });
        foreach (var item in items.Take(20))
        {
            var title = item.Titles?.FirstOrDefault()?.Value ?? item.TitleID ?? "Unknown";
            var ver = !string.IsNullOrEmpty(item.Version) ? $" v{item.Version}" : "";
            _detailsPanel.Children.Add(new TextBlock
            {
                Text = $"  • {title}{ver}",
                FontSize = 11,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(4, 0, 0, 1),
            });
        }
    }

    private void AddRelatedRomSection(string label, IList<DLC>? items)
    {
        if (items is null || items.Count == 0) return;
        _detailsPanel.Children.Add(new TextBlock
        {
            Text = $"{label} ({items.Count})",
            FontWeight = FontWeight.SemiBold,
            FontSize = 12,
            Margin = new Thickness(0, 8, 0, 4),
        });
        foreach (var item in items.Take(20))
        {
            var title = item.Titles?.FirstOrDefault()?.Value ?? item.TitleID ?? "Unknown";
            _detailsPanel.Children.Add(new TextBlock
            {
                Text = $"  • {title}",
                FontSize = 11,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(4, 0, 0, 1),
            });
        }
    }

    private void ClearRomDetails()
    {
        if (_detailsPanel is null) return;
        _detailsPanel.Children.Clear();
        _detailsPanel.Children.Add(new TextBlock
        {
            Text = "Select a ROM to see details.",
            Opacity = 0.6,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
        });
    }

    /// <summary>Loads an image from a URL asynchronously and sets it on the given Image control.</summary>
    private static async Task LoadImageFromUrlAsync(Avalonia.Controls.Image imageControl, string url)
    {
        try
        {
            var bytes = await _imgHttp.GetByteArrayAsync(url);
            using var ms = new MemoryStream(bytes);
            var bitmap = new Bitmap(ms);
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                imageControl.Source = bitmap;
                imageControl.Width = double.NaN;
                imageControl.Height = double.NaN;
            });
        }
        catch (Exception ex)
        {
            FileUtils.Log($"[Image] Failed to load URL '{url}': {ex.Message}");
        }
    }

    // ── Context menu ──────────────────────────────────────────────────────

    private ContextMenu BuildContextMenu(IConsoleViewModel console, DataGrid dataGrid)
    {
        var contextMenu = new ContextMenu();

        contextMenu.Opening += (_, _) =>
        {
            contextMenu.Items.Clear();

            var selectedVms = dataGrid.SelectedItems.OfType<RomViewModel>().ToList();
            var selectedRoms = selectedVms.Select(rv => rv.RomSource).ToList();

            if (selectedRoms.Count == 0)
            {
                contextMenu.Items.Add(new MenuItem { Header = "No ROM selected", IsEnabled = false });
                return;
            }

            bool single = selectedRoms.Count == 1;
            var firstVm = selectedVms[0];

            // ── File system ───────────────────────────────────────────────
            var openFolderItem = new MenuItem { Header = "📂 Open File Location", IsEnabled = single };
            openFolderItem.Click += (_, _) =>
            {
                var path = firstVm.Path;
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
            };
            contextMenu.Items.Add(openFolderItem);

            var openItem = new MenuItem { Header = "▶ Open with Default App", IsEnabled = single };
            openItem.Click += (_, _) =>
            {
                var path = firstVm.Path;
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
            };
            contextMenu.Items.Add(openItem);

            contextMenu.Items.Add(new Separator());

            // ── Clipboard ─────────────────────────────────────────────────
            var copyIdItem = new MenuItem
            {
                Header = single ? $"📋 Copy Title ID  ({firstVm.TitleID})" : $"📋 Copy Title IDs ({selectedVms.Count})"
            };
            copyIdItem.Click += async (_, _) =>
            {
                var ids = string.Join(", ", selectedVms.Select(v => v.TitleID));
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clipboard is not null) await clipboard.SetTextAsync(ids);
            };
            contextMenu.Items.Add(copyIdItem);

            var copyTitleItem = new MenuItem
            {
                Header = single ? $"📋 Copy Title" : $"📋 Copy Titles ({selectedVms.Count})"
            };
            copyTitleItem.Click += async (_, _) =>
            {
                var titles = string.Join(Environment.NewLine, selectedVms.Select(v => v.Title));
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clipboard is not null) await clipboard.SetTextAsync(titles);
            };
            contextMenu.Items.Add(copyTitleItem);

            var copyFileItem = new MenuItem { Header = "📋 Copy File Name", IsEnabled = single };
            copyFileItem.Click += async (_, _) =>
            {
                var fn = System.IO.Path.GetFileName(firstVm.Path);
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clipboard is not null && !string.IsNullOrEmpty(fn)) await clipboard.SetTextAsync(fn);
            };
            contextMenu.Items.Add(copyFileItem);

            // Copy cell value — uses the current column under focus
            var copyCellItem = new MenuItem { Header = "📋 Copy Cell Value" };
            copyCellItem.Click += async (_, _) =>
            {
                try
                {
                    var col = dataGrid.CurrentColumn;
                    if (col is not null && single)
                    {
                        var propName = col.Tag?.ToString()?.Split(':').LastOrDefault();
                        if (!string.IsNullOrEmpty(propName))
                        {
                            var value = firstVm.PropertiesDictionary.TryGetValue(propName, out var v) ? v : "";
                            if (string.IsNullOrEmpty(value))
                            {
                                value = propName switch
                                {
                                    "TitleID" => firstVm.TitleID,
                                    "Title" => firstVm.Title,
                                    "RomType" => firstVm.RomType,
                                    "Region" => firstVm.Region,
                                    "Version" => firstVm.Version,
                                    _ => ""
                                };
                            }
                            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                            if (clipboard is not null && !string.IsNullOrEmpty(value))
                                await clipboard.SetTextAsync(value);
                        }
                    }
                }
                catch (Exception ex) { FileUtils.Log($"[CopyCell] {ex.Message}"); }
            };
            contextMenu.Items.Add(copyCellItem);

            contextMenu.Items.Add(new Separator());

            // ── Organise ──────────────────────────────────────────────────
            var formatItem = new MenuItem { Header = "✏️ Format Name…", IsEnabled = single };
            formatItem.Click += async (_, _) => await ShowFormatNameDialogAsync(firstVm, console, dataGrid);
            contextMenu.Items.Add(formatItem);

            // Move to… — browse for target directory, enqueue via RomOperationService
            var moveItem = new MenuItem { Header = $"📦 Move to… ({selectedVms.Count})" };
            moveItem.Click += async (_, _) =>
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel is null) return;
                var storageProvider = topLevel.StorageProvider;
                var folders = await storageProvider.OpenFolderPickerAsync(
                    new Avalonia.Platform.Storage.FolderPickerOpenOptions
                    {
                        Title = "Select target directory",
                        AllowMultiple = false,
                    });
                if (folders.Count == 0) return;
                var targetRoot = folders[0].Path.LocalPath;

                if (DataContext is MainViewModel mainVm)
                {
                    var capturedRoms = selectedRoms.ToList();
                    var capturedVms = selectedVms.ToList();
                    var op = mainVm.OperationService.EnqueueMove(capturedRoms, targetRoot);

                    mainVm.Operations.OperationStateChanged += OnMoveDone;
                    void OnMoveDone(QueuedOperation changed)
                    {
                        if (changed.Id != op.Id || !changed.IsFinished) return;
                        mainVm.Operations.OperationStateChanged -= OnMoveDone;

                        if (changed.State == OperationState.Completed)
                        {
                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            {
                                foreach (var vm in capturedVms)
                                {
                                    var idx = console.Roms.IndexOf(vm);
                                    if (idx >= 0)
                                        console.Roms[idx] = new RomViewModel(vm.RomSource);
                                }
                            });
                        }
                    }

                    if (_bottomTabs is not null) _bottomTabs.SelectedIndex = 2;
                }
            };
            contextMenu.Items.Add(moveItem);

            var refreshMeta = new MenuItem { Header = "🔄 Refresh Metadata" };
            refreshMeta.Click += (_, _) => { _ = console.LoadData(); };
            contextMenu.Items.Add(refreshMeta);

            // ── Delete ────────────────────────────────────────────────────
            contextMenu.Items.Add(new Separator());
            var deleteItem = new MenuItem { Header = $"🗑 Delete ROM{(single ? "" : "s")} ({selectedVms.Count})" };
            deleteItem.Click += async (_, _) =>
            {
                var owner = TopLevel.GetTopLevel(this) as Window;
                var confirmDialog = new Window
                {
                    Title = "Confirm Delete",
                    Width = 400, Height = 130,
                    CanResize = false,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                var btns = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
                var yesBtn = new Button { Content = "Delete" };
                var noBtn = new Button { Content = "Cancel" };
                noBtn.Click += (_, _) => confirmDialog.Close();
                yesBtn.Click += (_, _) =>
                {
                    foreach (var vm in selectedVms)
                    {
                        try
                        {
                            if (File.Exists(vm.Path)) File.Delete(vm.Path);
                            console.Roms.Remove(vm);
                        }
                        catch (Exception ex) { console.StatusMessage = $"Delete failed: {ex.Message}"; }
                    }
                    confirmDialog.Close();
                };
                btns.Children.Add(noBtn);
                btns.Children.Add(yesBtn);
                var dsp = new StackPanel { Margin = new Thickness(16), Spacing = 10 };
                dsp.Children.Add(new TextBlock
                {
                    Text = $"Delete {selectedVms.Count} ROM file{(single ? "" : "s")} from disk?\nThis cannot be undone.",
                    TextWrapping = TextWrapping.Wrap,
                });
                dsp.Children.Add(btns);
                confirmDialog.Content = dsp;
                if (owner is not null) await confirmDialog.ShowDialog(owner);
            };
            contextMenu.Items.Add(deleteItem);

            // ── View All Properties ───────────────────────────────────────
            var viewPropsItem = new MenuItem { Header = "🔎 View All Properties…", IsEnabled = single };
            viewPropsItem.Click += async (_, _) =>
            {
                var owner = TopLevel.GetTopLevel(this) as Window;
                if (owner is null) return;
                var propsWin = new Window
                {
                    Title = $"Properties — {firstVm.Title}",
                    Width = 520, Height = 480,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
                var propsSp = new StackPanel { Margin = new Thickness(12), Spacing = 4 };
                foreach (var kv in firstVm.Properties)
                {
                    var row = new DockPanel { Margin = new Thickness(0, 1) };
                    var keyLbl = new TextBlock
                    {
                        Text = kv.Key + ":",
                        FontWeight = FontWeight.SemiBold, Width = 130, FontSize = 12,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                    };
                    DockPanel.SetDock(keyLbl, Dock.Left);
                    row.Children.Add(keyLbl);
                    var valBox = new TextBox
                    {
                        Text = kv.Value, IsReadOnly = true,
                        BorderThickness = new Thickness(0),
                        Background = Brushes.Transparent,
                        FontSize = 12,
                        TextWrapping = TextWrapping.Wrap,
                    };
                    row.Children.Add(valBox);
                    propsSp.Children.Add(row);
                }
                scroll.Content = propsSp;
                propsWin.Content = scroll;
                await propsWin.ShowDialog(owner);
            };
            contextMenu.Items.Add(viewPropsItem);

            // ── Console-specific + plugin actions ─────────────────────────
            var actions = console.GetContextActionsForSelection(selectedRoms)
                .GroupBy(a => a.Category)
                .ToList();

            if (actions.Count > 0)
            {
                contextMenu.Items.Add(new Separator());

                foreach (var group in actions)
                {
                    var header = new MenuItem { Header = group.Key, IsEnabled = false };
                    contextMenu.Items.Add(header);

                    foreach (var action in group)
                    {
                        var capturedAction = action;
                        var item = new MenuItem { Header = capturedAction.Name };

                        if (capturedAction.IconKey is not null
                            && Application.Current?.TryFindResource(capturedAction.IconKey, out var icon) == true
                            && icon is Avalonia.Media.Drawing drawing)
                        {
                            item.Icon = new Avalonia.Controls.Image
                            {
                                Source = new Avalonia.Media.DrawingImage(drawing),
                                Width = 16, Height = 16
                            };
                        }

                        item.Click += async (_, _) =>
                        {
                            try
                            {
                                if (DataContext is MainViewModel mainVm)
                                {
                                    var capturedRoms = selectedRoms.ToList();
                                    var capturedVms = selectedVms.ToList();

                                    // Core service enqueues the action — UI just calls
                                    var op = mainVm.OperationService.EnqueueAction(capturedAction, capturedRoms);

                                    // Listen for completion to refresh UI (view concern only)
                                    mainVm.Operations.OperationStateChanged += OnActionDone;
                                    void OnActionDone(QueuedOperation changed)
                                    {
                                        if (changed.Id != op.Id || !changed.IsFinished) return;
                                        mainVm.Operations.OperationStateChanged -= OnActionDone;

                                        if (changed.State == OperationState.Completed)
                                        {
                                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                                            {
                                                foreach (var vm in capturedVms)
                                                {
                                                    var idx = console.Roms.IndexOf(vm);
                                                    if (idx >= 0)
                                                        console.Roms[idx] = new RomViewModel(vm.RomSource);
                                                }
                                                if (capturedVms.Count == 1)
                                                    ShowRomDetails(new RomViewModel(capturedVms[0].RomSource));
                                            });
                                        }
                                    }

                                    // Switch to Operations tab for feedback
                                    if (_bottomTabs is not null) _bottomTabs.SelectedIndex = 2;
                                }
                            }
                            catch (Exception ex)
                            {
                                console.StatusMessage = $"Action failed: {ex.Message}";
                                FileUtils.Log($"[Action] '{capturedAction.Name}' failed: {ex.Message}");
                            }
                        };
                        contextMenu.Items.Add(item);
                    }
                }
            }
        };

        return contextMenu;
    }

    // ── Format Name dialog ────────────────────────────────────────────────

    private async Task ShowFormatNameDialogAsync(RomViewModel rvm, IConsoleViewModel console, DataGrid dataGrid)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner is null) return;

        var rom = rvm.RomSource;
        var template = Settings.Instance.RenameFormatString;

        // Use FileRenamer.GenerateFileName from core for preview
        string SafePreview(string fmt)
        {
            try { return FileRenamer.GenerateFileName(new[] { rom }, fmt); }
            catch { return "(invalid template)"; }
        }
        var proposed = SafePreview(template);

        var dialog = new Window
        {
            Title = "Format Name",
            Width = 600, Height = 340,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        var sp = new StackPanel { Margin = new Thickness(16), Spacing = 8 };

        sp.Children.Add(new TextBlock
        {
            Text = "Template (type or click placeholders below):",
            FontWeight = FontWeight.SemiBold,
            FontSize = 12,
        });
        var templateBox = new TextBox { Text = template, AcceptsReturn = false };
        sp.Children.Add(templateBox);

        // ── Clickable placeholder chips ──────────────────────────────────
        sp.Children.Add(new TextBlock { Text = "General:", FontSize = 11, Opacity = 0.7 });
        var genericChips = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
        foreach (var ph in new[] { "{TitleName}", "{TitleID}", "{Region}", "{Version}",
                                    "{Publisher}", "{Developer}", "{ReleaseDate}", "{Platform}", "{ProductCode}",
                                    "{Size}", "{Genres}", "{Players}", "{Type}", "{MinFirmware}", "{DLCCount}" })
        {
            var cap = ph;
            var chip = new Button { Content = cap, Padding = new Thickness(6, 2), Margin = new Thickness(0, 0, 4, 4), FontSize = 11 };
            chip.Click += (_, _) =>
            {
                var pos = templateBox.CaretIndex;
                var text = templateBox.Text ?? "";
                templateBox.Text = text.Insert(pos, cap);
                templateBox.CaretIndex = pos + cap.Length;
            };
            genericChips.Children.Add(chip);
        }
        sp.Children.Add(genericChips);

        // Console-specific property placeholders
        var consoleProps = console.GetColumnDescriptors()
            .Where(d => d.UsesDictionaryLookup)
            .Select(d => d.PropName)
            .ToList();
        if (consoleProps.Count > 0)
        {
            sp.Children.Add(new TextBlock { Text = $"{console.ConsoleName}:", FontSize = 11, Opacity = 0.7 });
            var consoleChips = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            foreach (var propName in consoleProps.Take(12))
            {
                var ph = "{" + propName + "}";
                var chip = new Button { Content = ph, Padding = new Thickness(6, 2), Margin = new Thickness(0, 0, 4, 4), FontSize = 11 };
                chip.Click += (_, _) =>
                {
                    var pos = templateBox.CaretIndex;
                    var text = templateBox.Text ?? "";
                    templateBox.Text = text.Insert(pos, ph);
                    templateBox.CaretIndex = pos + ph.Length;
                };
                consoleChips.Children.Add(chip);
            }
            sp.Children.Add(consoleChips);
        }

        var previewLbl = new TextBlock
        {
            Text = $"→  {proposed}",
            FontSize = 11,
            Opacity = 0.75,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0),
        };
        templateBox.TextChanged += (_, _) =>
            previewLbl.Text = $"→  {SafePreview(templateBox.Text ?? "")}";
        sp.Children.Add(previewLbl);

        var errorLbl = new TextBlock { Foreground = Brushes.Red, FontSize = 11, TextWrapping = TextWrapping.Wrap };
        sp.Children.Add(errorLbl);

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };

        var renameBtn = new Button { Content = "Rename" };
        renameBtn.Click += (_, _) =>
        {
            try
            {
                var fmt = templateBox.Text ?? template;

                if (DataContext is MainViewModel mainVm)
                {
                    // Core service creates and enqueues the operation — UI has no logic
                    var op = mainVm.OperationService.EnqueueRename(rom, fmt);

                    // Listen for completion to refresh the UI (view-only concern)
                    mainVm.Operations.OperationStateChanged += OnOpDone;
                    void OnOpDone(QueuedOperation changed)
                    {
                        if (changed.Id != op.Id || !changed.IsFinished) return;
                        mainVm.Operations.OperationStateChanged -= OnOpDone;

                        if (changed.State == OperationState.Completed)
                        {
                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            {
                                Settings.Instance.RenameFormatString = fmt;
                                Settings.Instance.Save();
                                var idx = console.Roms.IndexOf(rvm);
                                if (idx >= 0)
                                    console.Roms[idx] = new RomViewModel(rvm.RomSource);
                                ShowRomDetails(new RomViewModel(rvm.RomSource));
                            });
                        }
                    }

                    // Switch to operations tab
                    if (_bottomTabs is not null) _bottomTabs.SelectedIndex = 2;
                }

                dialog.Close();
            }
            catch (Exception ex)
            {
                errorLbl.Text = $"Error: {ex.Message}";
            }
        };
        var cancelBtn = new Button { Content = "Cancel" };
        cancelBtn.Click += (_, _) => dialog.Close();

        btnRow.Children.Add(cancelBtn);
        btnRow.Children.Add(renameBtn);
        sp.Children.Add(btnRow);

        dialog.Content = sp;
        await dialog.ShowDialog(owner);
    }
}

// ColumnManager class to manage static reference to current DataGrid
public class ColumnManager
{
    public static DataGrid? currentDataGrid { get; set; }
    public List<string> Groups { get; } = ["Rom Properties", "Details", "Technical"];
}

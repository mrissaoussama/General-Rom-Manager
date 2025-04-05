namespace AvaloniaUI.Views;

public class MainView : UserControl
{
    private TabControl _consoleTabs;
    private ColumnManager _columnManager;
    private Grid _mainGrid;
    private bool _initialized = false;

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

                // Watch for collection changes
                vm.ConsoleViewModels.CollectionChanged += (s, args) =>
                {
                    RefreshConsoleTabs();
                };

                // Immediately create tabs for any already loaded consoles
                RefreshConsoleTabs();
            }
        };
    }

    private void InitializeUI()
    {
        // Create main layout grid with two columns (tab area and side panel)
        _mainGrid = new Grid();
        _mainGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        _mainGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(0.2, GridUnitType.Star))); // 20% width

        // Create tab control
        _consoleTabs = new TabControl { Name = "ConsoleTabs" };
        _consoleTabs.SelectionChanged += OnTabSelectionChanged;

        // Create side panel
        var sidePanel = CreateSidePanel();

        // Add grid splitter for resizing
        var splitter = new GridSplitter
        {
            ResizeDirection = GridResizeDirection.Columns,
            Background = new SolidColorBrush(Colors.Gray),
            Width = 5
        };

        // Add elements to grid
        Grid.SetColumn(_consoleTabs, 0);
        _mainGrid.Children.Add(_consoleTabs);

        Grid.SetColumn(splitter, 0);
        Grid.SetColumnSpan(splitter, 2);
        splitter.HorizontalAlignment = HorizontalAlignment.Right;
        _mainGrid.Children.Add(splitter);

        Grid.SetColumn(sidePanel, 1);
        _mainGrid.Children.Add(sidePanel);

        // Set as content
        Content = _mainGrid;

        // Setup toolbar for column management
        SetupColumnsToolbar();
    }

    private void RefreshConsoleTabs()
    {
        if (DataContext is MainViewModel vm && _consoleTabs != null)
        {
            // Clear existing tabs
            _consoleTabs.Items.Clear();

            // Create tabs for all consoles
            foreach (var console in vm.ConsoleViewModels)
            {
                var tabItem = new TabItem
                {
                    Header = console.ConsoleName,
                    Content = CreateTabContent(console),
                    DataContext = console
                };
                _consoleTabs.Items.Add(tabItem);
            }

            // Select first tab if available
            if (_consoleTabs.Items.Count > 0)
                _consoleTabs.SelectedIndex = 0;
        }
    }

    private Control CreateTabContent(ConsoleViewModel console)
    {
        var dockPanel = new DockPanel();

        // Create top controls panel
        var topPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(5)
        };
        
        DockPanel.SetDock(topPanel, Dock.Top);
        
        // 1. Add menu for column visibility
        var menu = new Menu();
        var viewMenuItem = new MenuItem { Header = "View" };
        var columnsMenuItem = new MenuItem { Header = "Columns" };
        viewMenuItem.Items.Add(columnsMenuItem);
        menu.Items.Add(viewMenuItem);
        
        // Setup column menu item event to populate dynamically
        columnsMenuItem.AddHandler(PointerEnteredEvent, 
            (s, e) => PopulateColumnsMenu(columnsMenuItem, console.ConsoleName));
        
        // 2. Add refresh button
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Margin = new Thickness(5, 10, 0, 0)
        };
        
        var refreshButton = new Button { Content = "Refresh" };
        refreshButton.Click += (s, e) => console.LoadDataCommand.Execute(null);
        buttonPanel.Children.Add(refreshButton);
        
        // 3. Add status panel with progress bar
        var statusPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 10, 0, 0)
        };
        
        var progressBar = new ProgressBar
        {
            IsIndeterminate = true,
            Width = 100
        };
        
        progressBar.Bind(ProgressBar.IsVisibleProperty, 
            new Binding("IsLoading") { Source = console });
            
        var statusText = new TextBlock();
        statusText.Bind(TextBlock.TextProperty, 
            new Binding("StatusMessage") { Source = console });
        
        statusPanel.Children.Add(progressBar);
        statusPanel.Children.Add(statusText);
        
        // Add all elements to top panel
        topPanel.Children.Add(menu);
        topPanel.Children.Add(buttonPanel);
        topPanel.Children.Add(statusPanel);
        dockPanel.Children.Add(topPanel);
        
        // 4. Create DataGrid
        var dataGrid = new DataGrid
        {
            Name = "RomsDataGrid",
            AutoGenerateColumns = true,
            IsReadOnly = true,
            GridLinesVisibility = DataGridGridLinesVisibility.All,
            SelectionMode = DataGridSelectionMode.Single,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(5),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Colors.Gray),
            MinHeight = 300
        };
        
        dataGrid.Bind(DataGrid.ItemsSourceProperty, new Binding("Roms") { Source = console });
        
        // Initialize the DataGrid for this console view model
        console.InitializeDataGrid(dataGrid);
        
        dockPanel.Children.Add(dataGrid);
        
        return dockPanel;
    }
    
    private void PopulateColumnsMenu(MenuItem columnsMenuItem, string consoleName)
    {
        // Clear existing items
        columnsMenuItem.Items.Clear();

        // Get the settings for this console
        var settings = Settings.Instance;
        
        if (!settings.VisibleColumns.ContainsKey(consoleName))
        {
            settings.VisibleColumns[consoleName] = GetDefaultColumns();
            settings.Save();
        }
        
        // Get property info from Rom class using reflection
        var romProperties = typeof(Rom).GetProperties()
            .Where(p => p.GetCustomAttributes(typeof(UiPropertyAttribute), true).Length > 0)
            .OrderBy(p => ((UiPropertyAttribute)p.GetCustomAttributes(typeof(UiPropertyAttribute), true)[0]).Order);
        
        foreach (var property in romProperties)
        {
            var attr = (UiPropertyAttribute)property.GetCustomAttributes(typeof(UiPropertyAttribute), true)[0];
            
            if (!attr.Visible) continue;
            
            var checkBox = new CheckBox
            {
                Content = !string.IsNullOrEmpty(attr.DisplayName) ? attr.DisplayName : property.Name,
                IsChecked = settings.VisibleColumns[consoleName].Contains(property.Name)
            };
            
            // Handle column visibility changes
            var propertyName = property.Name;
            checkBox.IsCheckedChanged += (s, e) => {
                if (checkBox.IsChecked == true)
                {
                    if (!settings.VisibleColumns[consoleName].Contains(propertyName))
                        settings.VisibleColumns[consoleName].Add(propertyName);
                }
                else
                {
                    settings.VisibleColumns[consoleName].Remove(propertyName);
                }
                settings.Save();
                
                // Update visible columns in the currently displayed DataGrid
                if (_consoleTabs.SelectedItem is TabItem { Content: Control control })
                {
                    var dataGrid = control.FindDescendantOfType<DataGrid>();
                    var consoleVM = settings.VisibleColumns.Keys
                        .FirstOrDefault(c => c == consoleName);
                    
                    if (dataGrid != null && consoleVM != null)
                    {
                        // Find the view model for this tab
                        if (DataContext is MainViewModel mainVM)
                        {
                            var viewModel = mainVM.ConsoleViewModels
                                .FirstOrDefault(c => c.ConsoleName == consoleName);
                                
                            if (viewModel != null)
                            {
                                // Call the UpdateDataGridColumns method on the view model
                                viewModel.InitializeDataGrid(dataGrid);
                            }
                        }
                    }
                }
            };
            
            // Add to menu
            var menuItem = new MenuItem { Header = checkBox };
            columnsMenuItem.Items.Add(menuItem);
        }
    }

    private List<string> GetDefaultColumns()
    {
        return
        [
            "TitleID",
            "Title",
            "RomType",
            "Region",
            "Version"
        ];
    }

    private Control CreateSidePanel()
    {
        var sidePanel = new DockPanel();

        // Header for side panel
        var header = new TextBlock
        {
            Text = "ROM Details",
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(5)
        };
        DockPanel.SetDock(header, Dock.Top);
        sidePanel.Children.Add(header);

        // Content area for ROM details
        var detailsPanel = new StackPanel { Margin = new Thickness(5) };
        var detailsScroller = new ScrollViewer
        {
            Content = detailsPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        sidePanel.Children.Add(detailsScroller);

        return sidePanel;
    }

    private void SetupColumnsToolbar()
    {
        // Create a toolbar for column management
        var columnsToolbar = new DockPanel { LastChildFill = true };

        var columnsButton = new Button
        {
            Content = "Columns",
            Margin = new Thickness(5)
        };

        columnsButton.Click += (s, e) => { ShowColumnsMenu(); };

        DockPanel.SetDock(columnsButton, Dock.Left);
        columnsToolbar.Children.Add(columnsButton);

        // Add to main grid at the top
        var existingContent = _mainGrid.Children[0];
        _mainGrid.Children.RemoveAt(0);

        var contentPanel = new DockPanel();
        DockPanel.SetDock(columnsToolbar, Dock.Top);
        contentPanel.Children.Add(columnsToolbar);
        contentPanel.Children.Add(existingContent);

        Grid.SetColumn(contentPanel, 0);
        _mainGrid.Children.Insert(0, contentPanel);
    }

    private void ShowColumnsMenu()
    {
        if (_consoleTabs.SelectedItem is TabItem { Content: DockPanel dockPanel } tabItem)
        {
            var dataGrid = dockPanel.Children.OfType<DataGrid>().FirstOrDefault();
            if (dataGrid != null)
            {
                var consoleName = tabItem.Header.ToString();

                // Create flyout with column checkboxes
                var flyout = new Flyout();
                var columnsPanel = new StackPanel { Margin = new Thickness(10) };

                // Group columns
                var defaultGroup = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
                defaultGroup.Children.Add(new TextBlock
                {
                    Text = "Rom Properties",
                    FontWeight = FontWeight.Bold
                });

                foreach (var column in dataGrid.Columns)
                {
                    var checkBox = new CheckBox
                    {
                        Content = column.Header?.ToString() ?? "",
                        IsChecked = column.IsVisible,
                        Tag = column
                    };

                    checkBox.IsCheckedChanged += (s, e) =>
                    {
                        if (checkBox.Tag is DataGridColumn col && s is CheckBox cb)
                        {
                            col.IsVisible = cb.IsChecked ?? false;
                            SaveColumnVisibility(consoleName, col.Header?.ToString() ?? "", cb.IsChecked ?? false);
                        }
                    };

                    defaultGroup.Children.Add(checkBox);
                }

                columnsPanel.Children.Add(defaultGroup);

                // Add apply/reset buttons
                var buttonsPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                var resetButton = new Button { Content = "Reset" };
                resetButton.Click += (s, e) =>
                {
                    ResetColumnVisibility(consoleName, dataGrid);
                    flyout.Hide();
                };

                buttonsPanel.Children.Add(resetButton);
                columnsPanel.Children.Add(buttonsPanel);

                flyout.Content = columnsPanel;
                flyout.ShowAt((Control)_consoleTabs.SelectedItem);
            }
        }
    }

    private void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Console.WriteLine("Tab selection changed");

        if (_consoleTabs.SelectedItem is TabItem tabItem)
        {
            var consoleName = tabItem.Header.ToString();

            // Find DataGrid in current tab
            if (tabItem.Content is DockPanel dockPanel)
            {
                var dataGrid = dockPanel.Children.OfType<DataGrid>().FirstOrDefault();

                if (dataGrid != null)
                {
                    // Set current datagrid
                    ColumnManager.currentDataGrid = dataGrid;

                    // Try to get view model
                    if (tabItem.DataContext is ConsoleViewModel vm)
                    {
                        vm.InitializeDataGrid(dataGrid);
                    }
                    else if (DataContext is MainViewModel mainVM)
                    {
                        var consoleVM = mainVM.ConsoleViewModels.FirstOrDefault(c => c.ConsoleName == consoleName);
                        if (consoleVM != null) consoleVM.InitializeDataGrid(dataGrid);
                    }
                }
            }
        }
    }

    private bool IsColumnVisible(string consoleName, string columnName)
    {
        var settings = Settings.Instance;

        // Initialize if needed
        if (!settings.VisibleColumns.ContainsKey(consoleName))
        {
            settings.VisibleColumns[consoleName] = GetDefaultVisibleColumns();
            settings.Save();
        }

        return settings.VisibleColumns[consoleName].Contains(columnName);
    }

    private void SaveColumnVisibility(string consoleName, string columnName, bool isVisible)
    {
        var settings = Settings.Instance;

        // Initialize if needed
        if (!settings.VisibleColumns.ContainsKey(consoleName))
            settings.VisibleColumns[consoleName] = GetDefaultVisibleColumns();

        // Update setting
        if (isVisible && !settings.VisibleColumns[consoleName].Contains(columnName))
            settings.VisibleColumns[consoleName].Add(columnName);
        else if (!isVisible && settings.VisibleColumns[consoleName].Contains(columnName))
            settings.VisibleColumns[consoleName].Remove(columnName);

        settings.Save();
    }

    private void ResetColumnVisibility(string consoleName, DataGrid dataGrid)
    {
        var settings = Settings.Instance;
        settings.VisibleColumns[consoleName] = GetDefaultVisibleColumns();
        settings.Save();

        // Update UI
        foreach (var column in dataGrid.Columns)
        {
            var columnName = column.Header?.ToString() ?? "";
            column.IsVisible = settings.VisibleColumns[consoleName].Contains(columnName);
        }
    }

    private List<string> GetDefaultVisibleColumns()
    {
        // Return default list of columns that should be visible
        return
        [
            "TitleID",
            "Title",
            "RomType",
            "Region",
            "Version"
        ];
    }
}

// ColumnManager class to manage static reference to current DataGrid
public class ColumnManager
{
    public static DataGrid currentDataGrid { get; set; }

    // Column group management
    public List<string> Groups { get; } = ["Rom Properties", "Details", "Technical"];

    public IEnumerable<DataGridColumn> GetColumnsForGroup(string group)
    {
        // Map columns to groups
        switch (group)
        {
            case "Rom Properties":
                return GetRomPropertiesColumns();
            case "Details":
                return GetDetailsColumns();
            case "Technical":
                return GetTechnicalColumns();
            default:
                return new List<DataGridColumn>();
        }
    }

    private IEnumerable<DataGridColumn> GetRomPropertiesColumns()
    {
        // Core properties that should be in the first group
        var columns = new List<string> { "TitleID", "Title", "RomType", "Region", "Version" };
        return GetColumnsForProperties(columns);
    }

    private IEnumerable<DataGridColumn> GetDetailsColumns()
    {
        // Detailed information columns
        var columns = new List<string>
            { "Developer", "Publisher", "ReleaseDate", "Languages", "Genres", "NumberOfPlayers" };
        return GetColumnsForProperties(columns);
    }

    private IEnumerable<DataGridColumn> GetTechnicalColumns()
    {
        // Technical columns
        var columns = new List<string> { "Path", "Size", "ProductCode", "MinimumFirmware" };
        return GetColumnsForProperties(columns);
    }

    private IEnumerable<DataGridColumn> GetColumnsForProperties(List<string> properties)
    {
        var columns = new List<DataGridColumn>();

        if (currentDataGrid != null)
            foreach (var property in properties)
            {
                var column = currentDataGrid.Columns.FirstOrDefault(c =>
                    c.Header?.ToString() == property ||
                    (c as DataGridBoundColumn)?.Binding?.ToString()?.Contains($"[{property}]") == true);

                if (column != null) columns.Add(column);
            }

        return columns;
    }

    public bool IsColumnVisible(string columnName)
    {
        if (currentDataGrid == null) return false;

        // Get console name from current tab
        var tabControl = currentDataGrid.FindAncestorOfType<TabControl>();
        if (tabControl is { SelectedItem: TabItem tabItem })
        {
            var consoleName = tabItem.Header?.ToString();
            if (!string.IsNullOrEmpty(consoleName))
            {
                var settings = Settings.Instance;
                if (settings.VisibleColumns.TryGetValue(consoleName, out var visibleColumns))
                    return visibleColumns.Contains(columnName);
            }
        }

        return true; // Default to visible if can't determine
    }
}
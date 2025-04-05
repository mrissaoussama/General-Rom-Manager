namespace AvaloniaUI.ViewModels;

public partial class ConsoleViewModel : ViewModelBase
{
    private readonly Type _consoleType;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty] private ObservableCollection<RomViewModel> _roms = new();
    [ObservableProperty] private bool _isLoading = false;
    [ObservableProperty] private string _statusMessage = "Ready";
    [ObservableProperty] private RomViewModel _selectedRom;

    public string ConsoleName { get; }

    public ConsoleViewModel(string consoleName, Type consoleType, IServiceProvider serviceProvider)
    {
        ConsoleName = consoleName;
        _consoleType = consoleType;
        _serviceProvider =serviceProvider;
        
    }

    [RelayCommand]
    public async Task LoadData()
    {
        // Use reflection to invoke the generic method with the correct type
        var method = typeof(ConsoleViewModel).GetMethod(nameof(LoadDataForConsole), 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        var genericMethod = method.MakeGenericMethod(_consoleType);
        await (Task)genericMethod.Invoke(this, null);
    }
    private static async Task ProcessRomFiles<T>(ConsoleManager<T> manager, string romDirectory) where T : GamingConsole
    {
        var supportedExtensions = manager.RomParserExecutor.GetSupportedExtensions();
        var romFiles = FileUtils.GetFilesInDirectoryWithExtensions(romDirectory, supportedExtensions);

        Console.WriteLine($"Found {romFiles.Count} ROM files");
        foreach (var file in romFiles)
        {
            await manager.ProcessFile(file);
        }

        // If the TitleInfoProviderManager exists, enhance ROM title info.
        if (manager.TitleInfoProviderManager != null)
        {
            // (Assuming that GetTitleInfo returns an enhanced Rom.)
            for (int i = 0; i < manager.RomList.Count; i++)
            {
                manager.RomList[i] = await manager.TitleInfoProviderManager.GetTitleInfo(manager.RomList[i]);
            }
        }
    }
    private static async Task ProcessConsoleOperationsGeneric<T>(IServiceProvider services) where T : GamingConsole
    {
        // Resolve the concrete ConsoleManager<T> from DI.
        var manager = services.GetRequiredService<ConsoleManager<T>>();

        // Load any console-specific DLLs (using the type name).
        LoadConsoleSpecificDlls(typeof(T).Name);

        Console.WriteLine($"Setup: {manager.GetType()}");
        await manager.Setup();


    }
    private static List<IBaseRomOrganizer> GetAvailableOrganizers<T>(IServiceProvider services) where T : GamingConsole
    {
        var organizers = new List<IBaseRomOrganizer>();

        // Get console-specific organizers (generic)
        var specificOrganizers = services.GetServices<IRomOrganizer<T>>();
        organizers.AddRange(specificOrganizers);

        // Get non-generic organizers
        var genericOrganizers = services.GetServices<IRomOrganizer>();
        organizers.AddRange(genericOrganizers);

        return organizers;
    }
    private static void LoadConsoleSpecificDlls(string consoleType)
    {
        var consoleName = consoleType.Replace("Console", "");
        var tools = RomManagerConfiguration.Configuration
            .GetSection($"Consoles:{consoleName}:Tools")
            .GetChildren()
            .Select(t => t.Value)
            .ToList();

        foreach (var dll in tools)
        {
            var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dll);
            if (File.Exists(fullPath))
            {
                Assembly.LoadFrom(fullPath);
                Console.WriteLine($"Loaded {Path.GetFileName(fullPath)}");
            }
            else
            {
                Console.WriteLine($"Tool not found: {fullPath}");
            }
        }
    }
    // This method will be called with the correct type parameter
    private async Task LoadDataForConsole<T>() where T : GamingConsole
    {
        IsLoading = true;
        StatusMessage = "Loading ROMs...";

        try
        {  
            using var scope = _serviceProvider.CreateScope();

            // Get the console manager directly from service provider
            var manager = scope.ServiceProvider.GetRequiredService<ConsoleManager<T>>();
            
            // Clear existing ROM list
            manager.RomList.Clear();
            
            // Get ROM path from configuration
            var romPath = RomManagerConfiguration.GetConsoleRomPath(ConsoleName);
            
            if (string.IsNullOrEmpty(romPath) || !Directory.Exists(romPath))
            {
                StatusMessage = $"ROM path not found: {romPath}";
                IsLoading = false;
                return;
            }

            // Get supported file extensions
            var extensions = manager.RomParserExecutor.GetSupportedExtensions();
            
            if (extensions.Count == 0)
            {
                StatusMessage = "No ROM parsers available for this console";
                IsLoading = false;
                return;
            }

            // Find all ROM files
            var romFiles = FileUtils.GetFilesInDirectoryWithExtensions(romPath, extensions);
            
            // Process each file
            foreach (var file in romFiles)
            {
                await manager.ProcessFile(file);
            }
            
            // Update the view models
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Roms.Clear();
                foreach (var rom in manager.RomList)
                {
                    Roms.Add(new RomViewModel(rom));
                }
            });

            StatusMessage = $"Loaded {Roms.Count} ROMs";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading ROM data: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void InitializeDataGrid(DataGrid dataGrid)
    {
        if (dataGrid == null) return;
        
        // Clear existing columns
        dataGrid.Columns.Clear();
        
        // Get visible columns from settings
        var settings = Settings.Instance;
        var visibleColumns = settings.VisibleColumns.ContainsKey(ConsoleName) 
            ? settings.VisibleColumns[ConsoleName] 
            : new List<string> { "TitleID", "Title", "RomType", "Region", "Version" };
        
        // Add standard columns that all ROMs should have
        dataGrid.Columns.Add(new DataGridTextColumn 
        { 
            Header = "Title ID", 
            Binding = new Binding("TitleID"),
            IsVisible = visibleColumns.Contains("TitleID")
        });
        
        dataGrid.Columns.Add(new DataGridTextColumn 
        { 
            Header = "Title", 
            Binding = new Binding("Title"),
            IsVisible = visibleColumns.Contains("Title")
        });
        
        dataGrid.Columns.Add(new DataGridTextColumn 
        { 
            Header = "Type", 
            Binding = new Binding("RomType"),
            IsVisible = visibleColumns.Contains("RomType")
        });
        
        dataGrid.Columns.Add(new DataGridTextColumn 
        { 
            Header = "Region", 
            Binding = new Binding("Region"),
            IsVisible = visibleColumns.Contains("Region")
        });
        
        dataGrid.Columns.Add(new DataGridTextColumn 
        { 
            Header = "Version", 
            Binding = new Binding("Version"),
            IsVisible = visibleColumns.Contains("Version")
        });
        
        // Set up selection changed event
        dataGrid.SelectionChanged += (s, e) =>
        {
            if (dataGrid.SelectedItem is RomViewModel romViewModel)
            {
                SelectedRom = romViewModel;
            }
        };
    }
}
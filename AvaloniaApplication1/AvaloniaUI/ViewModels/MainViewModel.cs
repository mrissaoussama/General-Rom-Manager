namespace AvaloniaUI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private bool _configLoaded = false;
    public static Dictionary<Type, string> RomPaths = LoadConsolePaths();
    public List<Type> ConsoleTypes { get; set; }
    private static Dictionary<Type, string> LoadConsolePaths()
    {
        var consolePaths = new Dictionary<Type, string>();
        var assembly = Assembly.Load("RomManagerShared");

        foreach (var consoleSection in RomManagerConfiguration.Configuration.GetSection("Consoles").GetChildren())
        {
            var consoleName = consoleSection.Key + "Console"; // Match class name convention
            var consoleType = assembly.GetTypes().FirstOrDefault(t =>
                t.Name.Equals(consoleName, StringComparison.OrdinalIgnoreCase));

            if (consoleType != null)
            {
                var romPath = consoleSection["RomPath"];
                if (!string.IsNullOrEmpty(romPath))
                {
                    consolePaths.Add(consoleType, romPath);
                }
            }
        }

        return consolePaths;
    }
    [ObservableProperty] private ConsoleViewModel _selectedConsole;

    public ObservableCollection<ConsoleViewModel> ConsoleViewModels { get; } = new();

    public MainViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
ConsoleTypes = GetAvailableConsoleTypes();
        // Load console tabs from config
        Task.Run(LoadConfigAsync);
    }

    [RelayCommand]
    private async Task ReloadConfig()
    {
        await LoadConfigAsync();
    }

    [RelayCommand]
    private void Exit()
    {
        Environment.Exit(0);
    }

    [RelayCommand]
    private void CheckForUpdates()
    {
        // Implementation
    }

    [RelayCommand]
    private void RenameAllRoms()
    {
        // Implementation
    }

    [RelayCommand]
    private void OpenSettings()
    {
        // Implementation
    }


    [RelayCommand]
    private async Task Refresh()
    {
        _configLoaded = false;
        await LoadConfigAsync();
    
        // Load data for each console view model
        foreach (var console in ConsoleViewModels)
        {
            await console.LoadDataCommand.ExecuteAsync(null);
        }
    }
    public List<Type> GetAvailableConsoleTypes()
    {
        var assembly = Assembly.Load("RomManagerShared");
        return assembly.GetTypes()
            .Where(t => typeof(GamingConsole).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();
    }
// Update LoadConfigAsync method in MainViewModel.cs
    private async Task LoadConfigAsync()
    {
        // Return early if already loaded (unless refresh requested)
        if (_configLoaded)
            return;

        try
        {
            // Clear existing view models first
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                ConsoleViewModels.Clear();
                Console.WriteLine("Cleared existing console view models");
            });

            // Ensure configuration is loaded
            RomManagerShared.Configuration.RomManagerConfiguration.Load("config.json");

            // Load console types from shared assembly
            Console.WriteLine($"Found {ConsoleTypes.Count} console types");

            // Create console VMs
            var consoleVMs = new List<ConsoleViewModel>();
            foreach (var romPathEntry in RomPaths)
            {
                var consoleType = romPathEntry.Key;
                var consolePath = romPathEntry.Value;
                var consoleName = consoleType.Name.Replace("Console", "");

                if (string.IsNullOrEmpty(consolePath) || !Directory.Exists(consolePath))
                {
                    Console.WriteLine($"Skipping {consoleName}: Path not found or not configured");
                    continue;
                }

                Console.WriteLine($"Adding console: {consoleName} with path {consolePath}");
                var vm = new ConsoleViewModel(consoleName, consoleType, _serviceProvider);
                consoleVMs.Add(vm);
            }

            // Update on UI thread
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var vm in consoleVMs.OrderBy(vm => vm.ConsoleName)) ConsoleViewModels.Add(vm);

                Console.WriteLine($"Added {ConsoleViewModels.Count} console view models");

                // Select first tab if available
                if (ConsoleViewModels.Count > 0 && SelectedConsole == null)
                {
                    SelectedConsole = ConsoleViewModels[0];
                    Console.WriteLine($"Selected console: {SelectedConsole.ConsoleName}");
                }
            });

            _configLoaded = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading config: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
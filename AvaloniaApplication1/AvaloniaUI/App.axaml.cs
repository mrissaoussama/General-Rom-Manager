using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RomManagerShared;
using RomManagerShared.Base;
using RomManagerShared.Base.Database;
using RomManagerShared.Utils;
using System.Reflection;


namespace AvaloniaUI;

public partial class App : Application
{
    private IHost _host = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Load configuration first
        RomManagerConfiguration.Load("config.json");
        ConsoleModules.Load();

        var builder = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
                // Framework chatter (EF Core logs every SQL command at Information) stays out of the log.
                logging.AddFilter("Microsoft", LogLevel.Warning);
                logging.AddFilter("System", LogLevel.Warning);
                // Log file, log panel and parse diagnostics.
                logging.AddProvider(new RomManagerShared.Logging.LogFileProvider());
            })
            .ConfigureServices((hostContext, services) =>
            {
                // config.json as IOptionsMonitor<AppOptions> (reloads with the files)
                services.Configure<AppOptions>(RomManagerConfiguration.Configuration);

                // Register all console-related services
                ConsoleModules.Register(services);

                // EF Core — scoped so each UI action gets its own context
                services.AddDbContext<AppDbContext>(options => { /* configured in OnConfiguring */ },
                    ServiceLifetime.Scoped);

                // Unit of Work is the same scoped DbContext instance
                services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

                // Shared utilities
                services.AddScoped<RomHashRepository>();
                services.AddScoped<LibraryScanner>();
                services.AddSingleton<ILibraryRoots, LibraryRoots>();
                services.AddSingleton(_ =>
                {
                    var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                    http.DefaultRequestHeaders.UserAgent.ParseAdd("RomManager/1.0");
                    return new ImageCache(Path.Combine(RomManagerConfiguration.GetCachePath(), "Images"), http);
                });

                // Plugin manager — singleton so plugins stay loaded for the app lifetime
                services.AddSingleton<PluginManager>();
            });

        _host = builder.Build();

        var covers = _host.Services.GetRequiredService<ImageCache>();
        covers.AllowDownloads = Settings.Instance.DownloadBoxArt;
        RomViewModel.CoverCache = covers;
        RomManagerShared.Utils.Dat.DiscHashing.UserOverride = Settings.Instance.HashDiscImages;
        if (ConsoleOptionsStore.MigrateLegacy(Settings.Instance)) Settings.Instance.Save();
        ConsoleOptionsStore.ApplyAll(_host.Services.GetServices<IConsoleOptions>(), Settings.Instance);

        // Core-library logging (AppLog) goes through the host's providers.
        RomManagerShared.Logging.AppLog.Factory = _host.Services.GetRequiredService<ILoggerFactory>();

        // Create/upgrade the SQLite database before anything queries it; a broken one is moved aside and rebuilt.
        string? brokenDatabase = null;
        string? databaseError = null;
        try
        {
            brokenDatabase = DatabaseStartup.MigrateOrReset(() => new AppDbContext(), AppDbContext.GetDbPath());
        }
        catch (Exception ex)
        {
            databaseError = ex.Message;
            AppLog.Error("The library database cannot be opened", ex);
        }

        // Load plugins asynchronously
        Task.Run(async () =>
        {
            try
            {
                var pluginDir = RomManagerConfiguration.GetPluginsPath();
                var pluginManager = _host.Services.GetRequiredService<PluginManager>();
                // Forward UI requests from plugins onto the Avalonia UI thread
                pluginManager.ShowPluginUICallback = async (control, title) =>
                    await Dispatcher.UIThread.InvokeAsync(() => ShowPluginUI(control, title));
                pluginManager.ApprovalCallback = request => Dispatcher.UIThread.InvokeAsync(() => DialogService.Current.ConfirmAsync(
                    "Allow plugin?",
                    $"{request.Name} {request.Version}" + (request.Author is null ? "" : $" by {request.Author}") + Environment.NewLine
                    + (request.Description is null ? "" : request.Description + Environment.NewLine) + Environment.NewLine
                    + "Permissions: " + (request.Permissions.Count == 0 ? Strings.App_None : string.Join(", ", request.Permissions)) + Environment.NewLine
                    + string.Format(Strings.App_File, request.DllPath) + Environment.NewLine + Environment.NewLine
                    + Strings.App_PluginsRunWithYourUserAccountSAccessOnlyAllowPlu,
                    Strings.App_Allow));
                await pluginManager.LoadPluginsAsync(pluginDir);
            }
            catch (Exception ex)
            {
                AppLog.Error("Plugin loading failed", ex);
            }
        });

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Apply persisted theme before the first window opens
            ApplyThemeFromSettings();


            var scope = _host.Services.CreateScope();
            var mainVm = new MainViewModel(scope.ServiceProvider) { Updater = new VelopackUpdater() };
            var s = Settings.Instance;
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainVm,
                Width  = s.WindowWidth  > 0 ? s.WindowWidth  : 1280,
                Height = s.WindowHeight > 0 ? s.WindowHeight : 800,
                Title  = "RomManager",
            };

            RestoreWindowPlacement(desktop.MainWindow, s);

            // Show onboarding wizard on first run (after the main window has rendered)
            desktop.MainWindow.Opened += async (_, _) =>
            {
                var databaseNotice = brokenDatabase is not null ? string.Format(Strings.App_DatabaseReset, brokenDatabase)
                    : databaseError is not null ? string.Format(Strings.App_DatabaseUnavailable, AppDbContext.GetDbPath(), databaseError)
                    : null;
                if (databaseNotice is not null)
                    await DialogService.Current.PreviewAsync(Strings.App_DatabaseResetTitle, databaseNotice, [], Strings.Console_Close, showCancel: false);
                if (OnboardingViewModel.ShouldShow(Settings.Instance, _host.Services.GetRequiredService<ILibraryRoots>()))
                    await DialogService.Current.ShowAsync(new OnboardingViewModel(Settings.Instance));
                // Cached libraries show immediately; only new/changed files are parsed.
                await mainVm.RefreshCommand.ExecuteAsync(null);
                mainVm.ScheduleRescans(Settings.Instance.RescanIntervalHours);
                if (Settings.Instance.CheckForAppUpdates)
                    await mainVm.CheckForAppUpdateAsync(quiet: true);
            };

            // Persist window size on close
            desktop.MainWindow.Closing += (_, _) =>
            {
                if (desktop.MainWindow is { } win)
                {
                    s.WindowMaximized = win.WindowState == WindowState.Maximized;
                    if (win.WindowState == WindowState.Normal)
                    {
                        s.WindowWidth  = (int)win.Width;
                        s.WindowHeight = (int)win.Height;
                        s.WindowX = win.Position.X;
                        s.WindowY = win.Position.Y;
                    }
                    s.Save();
                }
            };
            desktop.ShutdownRequested += async (_, _) =>
            {
                var pm = _host.Services.GetService<PluginManager>();
                if (pm is not null) await pm.DisposeAsync();
                await _host.StopAsync();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Default plugin UI handler — opens a window with the plugin's control.
    /// Replace this with a more integrated panel in the main shell if desired.
    /// </summary>
    private static Task ShowPluginUI(object control, string title)
    {
        // Accept either an Avalonia Control or a plain string from plugins
        Avalonia.Controls.Control content = control switch
        {
            Avalonia.Controls.Control c => c,
            string s => new ScrollViewer
            {
                Content = new TextBlock
                {
                    Text = s,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Margin = new Avalonia.Thickness(16),
                    FontFamily = new Avalonia.Media.FontFamily("Consolas,Monospace"),
                    FontSize = 13,
                }
            },
            _ => new TextBlock { Text = control?.ToString() ?? Strings.App_Empty }
        };

        var win = new Window
        {
            Title = title,
            Content = content,
            Width = 640,
            Height = 480,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        win.Show();
        return Task.CompletedTask;
    }

    // Restores the last position only if it is still on a connected screen (monitors change).
    private static void RestoreWindowPlacement(Window win, Settings s)
    {
        if (s.WindowX is { } x && s.WindowY is { } y)
        {
            var topLeft = new PixelPoint(x, y);
            var onScreen = win.Screens.All.Any(sc => sc.WorkingArea.Contains(topLeft + new PixelPoint(40, 20)));
            if (onScreen)
            {
                win.WindowStartupLocation = WindowStartupLocation.Manual;
                win.Position = topLeft;
            }
        }
        if (s.WindowMaximized) win.WindowState = WindowState.Maximized;
    }

    private void ApplyThemeFromSettings()
    {
        var variant = Settings.Instance.Theme switch
        {
            "Dark"  => Avalonia.Styling.ThemeVariant.Dark,
            "Light" => Avalonia.Styling.ThemeVariant.Light,
            _       => Avalonia.Styling.ThemeVariant.Default,
        };
        RequestedThemeVariant = variant;
    }

}

using RomManagerShared.Switch.Configuration;
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
                // File logging hook: bridge Microsoft.Extensions.Logging into FileUtils
                logging.AddProvider(new FileUtilsLoggerProvider());
            })
            .ConfigureServices((hostContext, services) =>
            {
                // Register all console-related services
                ConsoleModules.Register(services);

                // EF Core — scoped so each UI action gets its own context
                services.AddDbContext<AppDbContext>(options => { /* configured in OnConfiguring */ },
                    ServiceLifetime.Scoped);

                // Unit of Work is the same scoped DbContext instance
                services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

                // Shared utilities
                services.AddScoped<RomHashRepository>();
                services.AddScoped<NoIntroRomHashIdentifier>();
                services.AddScoped<LibraryScanner>();
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

        RomViewModel.CoverCache = _host.Services.GetRequiredService<ImageCache>();

        // Wire FileUtils static logger to the DI logger factory
        var loggerFactory = _host.Services.GetRequiredService<ILoggerFactory>();
        FileUtils.Logger = loggerFactory.CreateLogger("FileUtils");

        // Create/upgrade the SQLite database before anything queries it.
        using (var dbScope = _host.Services.CreateScope())
        {
            try { dbScope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate(); }
            catch (Exception ex) { FileUtils.Log($"Database migration failed: {ex.Message}"); }
        }

        // Load plugins asynchronously
        Task.Run(async () =>
        {
            try
            {
                var pluginDir = RomManagerConfiguration.GetGlobalPath("Plugins");
                var pluginManager = _host.Services.GetRequiredService<PluginManager>();
                // Forward UI requests from plugins onto the Avalonia UI thread
                pluginManager.ShowPluginUICallback = async (control, title) =>
                    await Dispatcher.UIThread.InvokeAsync(() => ShowPluginUI(control, title));
                await pluginManager.LoadPluginsAsync(pluginDir);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Plugin loading failed: {ex.Message}");
            }
        });

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Apply persisted theme before the first window opens
            ApplyThemeFromSettings();

            // Push user URL/path overrides from Settings → SwitchConfiguration
            ApplySwitchConfigOverrides();

            var scope = _host.Services.CreateScope();
            var mainVm = new MainViewModel(scope.ServiceProvider);
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
                await OnboardingWindow.ShowIfFirstRunAsync(desktop.MainWindow);
                // Cached libraries show immediately; only new/changed files are parsed.
                await mainVm.RefreshCommand.ExecuteAsync(null);
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
            _ => new TextBlock { Text = control?.ToString() ?? "(empty)" }
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

    private static void ApplySwitchConfigOverrides()
    {
        var s = Settings.Instance;
        if (!string.IsNullOrWhiteSpace(s.SwitchTitleDBPathOverride))
            SwitchConfiguration.TitleDBPathOverride = s.SwitchTitleDBPathOverride;
        if (!string.IsNullOrWhiteSpace(s.SwitchTitleDBUrlOverride))
            SwitchConfiguration.TitleDBUrlOverride = s.SwitchTitleDBUrlOverride;
        if (!string.IsNullOrWhiteSpace(s.SwitchVersionsUrlOverride))
            SwitchConfiguration.VersionsUrlOverride = s.SwitchVersionsUrlOverride;
    }
}

// ── Minimal ILoggerProvider to route Microsoft.Extensions.Logging → FileUtils ─

internal sealed class FileUtilsLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new FileUtilsLogger(categoryName);
    public void Dispose() { }
}

internal sealed class FileUtilsLogger(string categoryName) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;
        var msg = $"[{logLevel}] [{categoryName}] {formatter(state, exception)}";
        if (exception is not null) msg += $"\n{exception}";
        FileUtils.Log(msg);
    }
}

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
        var assembly = Assembly.Load("RomManagerShared");

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
                RegisterConsoleServices(services, assembly);

                // EF Core — scoped so each UI action gets its own context
                services.AddDbContext<AppDbContext>(options => { /* configured in OnConfiguring */ },
                    ServiceLifetime.Scoped);

                // Unit of Work is the same scoped DbContext instance
                services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

                // Shared utilities
                services.AddScoped<RomHashRepository>();
                services.AddScoped<NoIntroRomHashIdentifier>();

                // Plugin manager — singleton so plugins stay loaded for the app lifetime
                services.AddSingleton<PluginManager>();
            });

        _host = builder.Build();

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

            // Show onboarding wizard on first run (after the main window has rendered)
            desktop.MainWindow.Opened += async (_, _) =>
            {
                await OnboardingWindow.ShowIfFirstRunAsync(desktop.MainWindow);
            };

            // Persist window size on close
            desktop.MainWindow.Closing += (_, _) =>
            {
                if (desktop.MainWindow is { } win)
                {
                    s.WindowWidth  = (int)win.Width;
                    s.WindowHeight = (int)win.Height;
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

    private static void RegisterConsoleServices(IServiceCollection services, Assembly assembly)
    {
        var gamingConsoleType = typeof(GamingConsole);

        var consoleTypes = assembly.GetTypes()
            .Where(t => gamingConsoleType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();

        // Non-generic IRomOrganizer implementations
        foreach (var organizerType in assembly.GetTypes()
            .Where(t => typeof(IRomOrganizer).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract))
        {
            services.AddScoped(typeof(IRomOrganizer), organizerType);
        }

        foreach (var consoleType in consoleTypes)
        {
            // Per-console generic services
            services.AddScoped(typeof(GenericRepository<>).MakeGenericType(consoleType));
            services.AddScoped(typeof(RomParserExecutor<>).MakeGenericType(consoleType));

            // TitleInfoProviderManager<T> aggregates all ITitleInfoProvider<T> implementations
            services.AddScoped(typeof(TitleInfoProviderManager<>).MakeGenericType(consoleType));

            RegisterOpenGeneric(services, assembly, typeof(ITitleInfoProvider<>), consoleType);
            RegisterOpenGeneric(services, assembly, typeof(IRomParser<>), consoleType);
            RegisterOpenGeneric(services, assembly, typeof(IUpdateVersionProvider<>), consoleType);
            RegisterOpenGeneric(services, assembly, typeof(IRomOrganizer<>), consoleType);
            RegisterOpenGeneric(services, assembly, typeof(ILicenseOrganizer<>), consoleType);

            // ConsoleManager<T> — use specific subclass if registered, otherwise generic base
            var consoleManagerType = typeof(ConsoleManager<>).MakeGenericType(consoleType);

            // Check for a console-specific manager (e.g. SwitchManager : ConsoleManager<SwitchConsole>)
            var specificManager = assembly.GetTypes()
                .FirstOrDefault(t => !t.IsAbstract && consoleManagerType.IsAssignableFrom(t)
                                  && t != consoleManagerType);

            if (specificManager is not null)
            {
                services.AddScoped(consoleManagerType, specificManager);
                // Also register the specific manager by its own type so
                // GetRequiredService<SwitchManager>() etc. works from actions.
                services.AddScoped(specificManager);
            }
            else
                services.AddScoped(consoleManagerType);
        }

        // Some TitleInfoProviders take GenericRepository<SomeDtoType> (e.g. SwitchJsonRomDTO).
        // Those DTO types are NOT GamingConsole subtypes, so they need a separate scan.
        RegisterDtoRepositories(services, assembly);
    }

    /// <summary>
    /// Scans all concrete types in <paramref name="assembly"/> for constructor parameters of type
    /// <c>GenericRepository&lt;T&gt;</c> and registers each discovered closed type as scoped.
    /// This ensures DTO-backed repositories (e.g. GenericRepository&lt;SwitchJsonRomDTO&gt;) are
    /// available without having to list them explicitly.
    /// </summary>
    private static void RegisterDtoRepositories(IServiceCollection services, Assembly assembly)
    {
        var genericRepoOpen = typeof(GenericRepository<>);
        var alreadyRegistered = new HashSet<Type>();

        foreach (var type in assembly.GetTypes().Where(t => !t.IsAbstract && !t.IsInterface))
        {
            foreach (var ctor in type.GetConstructors())
            {
                foreach (var param in ctor.GetParameters())
                {
                    var pt = param.ParameterType;
                    if (pt.IsGenericType
                        && pt.GetGenericTypeDefinition() == genericRepoOpen
                        && alreadyRegistered.Add(pt))
                    {
                        services.AddScoped(pt);
                    }
                }
            }
        }
    }

    private static void RegisterOpenGeneric(
        IServiceCollection services, Assembly assembly,
        Type openInterface, Type consoleType)
    {
        var closedInterface = openInterface.MakeGenericType(consoleType);
        foreach (var impl in assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && closedInterface.IsAssignableFrom(t)))
        {
            services.AddScoped(closedInterface, impl);
        }
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

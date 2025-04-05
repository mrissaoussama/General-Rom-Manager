using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Hosting;
using RomManagerShared;
using RomManagerShared.Base.Database;

namespace AvaloniaUI;

public partial class App : Application
{    private IHost _host;


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
        .ConfigureServices((hostContext, services) =>
        {
            // Register all console-related services.
            RegisterConsoleServices(services, assembly);
            // Register common services.
            services.AddDbContext<AppDbContext>();
            services.AddScoped<RomHashRepository>();
            services.AddScoped<NoIntroRomHashIdentifier>();
        });

    _host = builder.Build();



    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        // Create a scope here and pass its ServiceProvider to your view model.
        var scope = _host.Services.CreateScope();
        desktop.MainWindow = new MainWindow
        {
            DataContext = new MainViewModel(scope.ServiceProvider)
        };
    }

    base.OnFrameworkInitializationCompleted();
}



  private static void RegisterConsoleServices(IServiceCollection services, Assembly assembly)
        {
            var gamingConsoleType = typeof(GamingConsole);

            // Find all gaming console types.
            var consoleTypes = assembly.GetTypes()
                .Where(t => gamingConsoleType.IsAssignableFrom(t)
                            && !t.IsInterface && !t.IsAbstract)
                .ToList();

            // Register non-generic IRomOrganizer implementations.
            var nonGenericOrganizerTypes = assembly.GetTypes()
                .Where(t => typeof(IRomOrganizer).IsAssignableFrom(t)
                            && !t.IsInterface && !t.IsAbstract)
                .ToList();

            foreach (var organizerType in nonGenericOrganizerTypes)
            {
                services.AddScoped(typeof(IRomOrganizer), organizerType);
            }

            foreach (var consoleType in consoleTypes)
            {
                // Register repositories and executors.
                var genericRepositoryType = typeof(GenericRepository<>).MakeGenericType(consoleType);
                var romParserExecutorType = typeof(RomParserExecutor<>).MakeGenericType(consoleType);

                services.AddScoped(genericRepositoryType);
                services.AddScoped(romParserExecutorType);

                // Register all ITitleInfoProvider implementations for the console.
                var titleInfoProviderInterface = typeof(ITitleInfoProvider<>).MakeGenericType(consoleType);
                var titleInfoProviderTypes = assembly.GetTypes()
                    .Where(t => titleInfoProviderInterface.IsAssignableFrom(t)
                                && !t.IsInterface && !t.IsAbstract)
                    .ToList();

                foreach (var titleInfoProviderType in titleInfoProviderTypes)
                {
                    services.AddScoped(titleInfoProviderInterface, titleInfoProviderType);
                }

                // Register all IRomParser implementations for the console.
                var romParserInterface = typeof(IRomParser<>).MakeGenericType(consoleType);
                var romParserTypes = assembly.GetTypes()
                    .Where(t => romParserInterface.IsAssignableFrom(t)
                                && !t.IsInterface && !t.IsAbstract)
                    .ToList();

                foreach (var parserType in romParserTypes)
                {
                    services.AddScoped(romParserInterface, parserType);
                }

                // Register specific services like IUpdateVersionProvider.
                var updateVersionProviderInterface = typeof(IUpdateVersionProvider<>).MakeGenericType(consoleType);
                var updateVersionProviderTypes = assembly.GetTypes()
                    .Where(t => updateVersionProviderInterface.IsAssignableFrom(t)
                                && !t.IsInterface && !t.IsAbstract)
                    .ToList();

                foreach (var versionProviderType in updateVersionProviderTypes)
                {
                    services.AddScoped(updateVersionProviderInterface, versionProviderType);
                }

                // Register ConsoleManager<T>.
                var consoleManagerGenericType = typeof(ConsoleManager<>).MakeGenericType(consoleType);
                services.AddScoped(consoleManagerGenericType);

                // Register console-specific IRomOrganizer<T> implementations.
                var romOrganizerInterface = typeof(IRomOrganizer<>).MakeGenericType(consoleType);
                var romOrganizerTypes = assembly.GetTypes()
                    .Where(t => romOrganizerInterface.IsAssignableFrom(t)
                                && !t.IsInterface && !t.IsAbstract)
                    .ToList();

                foreach (var organizerType in romOrganizerTypes)
                {
                    services.AddScoped(romOrganizerInterface, organizerType);
                }

                // Register ILicenseOrganizer<T> implementations if required.
                var licenseOrganizerInterface = typeof(ILicenseOrganizer<>).MakeGenericType(consoleType);
                var licenseOrganizerTypes = assembly.GetTypes()
                    .Where(t => licenseOrganizerInterface.IsAssignableFrom(t)
                                && !t.IsInterface && !t.IsAbstract)
                    .ToList();

                foreach (var licenseOrganizerType in licenseOrganizerTypes)
                {
                    services.AddScoped(licenseOrganizerInterface, licenseOrganizerType);
                }
            }

        }



}
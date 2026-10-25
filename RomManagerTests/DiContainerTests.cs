using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RomManagerShared;
using RomManagerShared.Base;
using RomManagerShared.Base.Database;
using RomManagerShared.Configuration;
using RomManagerShared.Interfaces;
using System.Reflection;

namespace RomManagerTests;

/// <summary>
/// Validates that the DI container can resolve all ConsoleManager&lt;T&gt; types
/// without throwing. This acts as a regression guard for the type of crash
/// described in bug: "Unable to resolve TitleInfoProviderManager – Switch console".
/// </summary>
public class DiContainerTests
{
    // ── Container builder (mirrors App.axaml.cs RegisterConsoleServices) ─

    private static IServiceProvider BuildContainer()
    {
        // Load an empty configuration so that provider constructors that call
        // RomManagerConfiguration.Configuration.GetSection(...) don't NullRef.
        // The optional JSON file is absent in test context; that is fine.
        RomManagerConfiguration.Load("config.json");

        var services = new ServiceCollection();
        var assembly = Assembly.Load("RomManagerShared");

        // EF Core (in-memory for tests)
        services.AddDbContext<AppDbContext>(
            opt => opt.UseInMemoryDatabase("DiTest"),
            ServiceLifetime.Scoped);
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        RegisterConsoleServices(services, assembly);

        return services.BuildServiceProvider();
    }

    private static void RegisterConsoleServices(IServiceCollection services, Assembly assembly)
    {
        var gamingConsoleType = typeof(GamingConsole);
        var consoleTypes = assembly.GetTypes()
            .Where(t => gamingConsoleType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();

        foreach (var consoleType in consoleTypes)
        {
            services.AddScoped(typeof(GenericRepository<>).MakeGenericType(consoleType));
            services.AddScoped(typeof(RomParserExecutor<>).MakeGenericType(consoleType));

            // This was the missing registration that caused the runtime crash.
            services.AddScoped(typeof(TitleInfoProviderManager<>).MakeGenericType(consoleType));

            RegisterOpenGeneric(services, assembly, typeof(ITitleInfoProvider<>), consoleType);
            RegisterOpenGeneric(services, assembly, typeof(IRomParser<>), consoleType);

            var consoleManagerType = typeof(ConsoleManager<>).MakeGenericType(consoleType);
            var specificManager = assembly.GetTypes()
                .FirstOrDefault(t => !t.IsAbstract
                                  && consoleManagerType.IsAssignableFrom(t)
                                  && t != consoleManagerType);

            if (specificManager is not null)
                services.AddScoped(consoleManagerType, specificManager);
            else
                services.AddScoped(consoleManagerType);
        }

        // Register GenericRepository<T> for DTO types used by TitleInfoProviders
        // (e.g. GenericRepository<SwitchJsonRomDTO> needed by SwitchJsonTitleInfoProvider)
        RegisterDtoRepositories(services, assembly);
    }

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

    // ── Tests ─────────────────────────────────────────────────────────────

    [Fact]
    public void AllConsoleManagers_CanBeResolved_WithoutException()
    {
        // Arrange
        var provider = BuildContainer();
        var consoleTypes = Assembly.Load("RomManagerShared").GetTypes()
            .Where(t => typeof(GamingConsole).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();

        consoleTypes.Should().NotBeEmpty("at least one GamingConsole subtype must exist in RomManagerShared");

        // Act & Assert
        using var scope = provider.CreateScope();
        foreach (var consoleType in consoleTypes)
        {
            var managerType = typeof(ConsoleManager<>).MakeGenericType(consoleType);
            var act = () => scope.ServiceProvider.GetRequiredService(managerType);
            act.Should().NotThrow(
                $"ConsoleManager<{consoleType.Name}> must be resolvable from the DI container");
        }
    }

    [Fact]
    public void AllTitleInfoProviderManagers_CanBeResolved()
    {
        // Arrange
        var provider = BuildContainer();
        var consoleTypes = Assembly.Load("RomManagerShared").GetTypes()
            .Where(t => typeof(GamingConsole).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();

        // Act & Assert
        using var scope = provider.CreateScope();
        foreach (var consoleType in consoleTypes)
        {
            var mgrType = typeof(TitleInfoProviderManager<>).MakeGenericType(consoleType);
            var act = () => scope.ServiceProvider.GetRequiredService(mgrType);
            act.Should().NotThrow(
                $"TitleInfoProviderManager<{consoleType.Name}> must be resolvable — " +
                "missing registration was the root cause of the Switch console crash");
        }
    }

    [Fact]
    public void AllRomParserExecutors_CanBeResolved()
    {
        var provider = BuildContainer();
        var consoleTypes = Assembly.Load("RomManagerShared").GetTypes()
            .Where(t => typeof(GamingConsole).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();

        using var scope = provider.CreateScope();
        foreach (var consoleType in consoleTypes)
        {
            var executorType = typeof(RomParserExecutor<>).MakeGenericType(consoleType);
            var act = () => scope.ServiceProvider.GetRequiredService(executorType);
            act.Should().NotThrow($"RomParserExecutor<{consoleType.Name}> must be resolvable");
        }
    }
}

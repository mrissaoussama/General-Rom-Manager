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
[Collection("ConsoleModules")]
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

        // EF Core (in-memory for tests)
        services.AddDbContext<AppDbContext>(
            opt => opt.UseInMemoryDatabase("DiTest"),
            ServiceLifetime.Scoped);
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        ConsoleModules.Register(services);

        return services.BuildServiceProvider();
    }

    // ── Tests ─────────────────────────────────────────────────────────────

    [Fact]
    public void AllConsoleManagers_CanBeResolved_WithoutException()
    {
        // Arrange
        var provider = BuildContainer();
        var consoleTypes = ConsoleModules.ConsoleTypes
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
        var consoleTypes = ConsoleModules.ConsoleTypes
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
        var consoleTypes = ConsoleModules.ConsoleTypes
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

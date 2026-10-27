using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RomManagerShared.Base;
using RomManagerShared.Base.Database;
using RomManagerShared.Base.Library;
using RomManagerShared.Interfaces;
using RomManagerShared.Switch;

namespace RomManagerTests;

[CollectionDefinition("ConsoleModules", DisableParallelization = true)]
public sealed class ConsoleModulesCollection;

/// <summary>Deleting a console module DLL must leave the app working with the other consoles.</summary>
[Collection("ConsoleModules")]
public sealed class MissingModuleTests : IDisposable
{
    private readonly IReadOnlyList<System.Reflection.Assembly> _all;

    public MissingModuleTests()
    {
        _all = [.. ConsoleModules.Assemblies.Skip(2)]; // core + abstractions come first
        ConsoleModules.SetLoadedModules(_all.Where(a => a != typeof(SwitchConsole).Assembly));
    }

    public void Dispose() => ConsoleModules.SetLoadedModules(_all);

    [Fact]
    public void WithoutTheSwitchModule_TheOtherConsolesRegisterAndResolve()
    {
        ConsoleModules.ConsoleTypes.Should().NotContain(typeof(SwitchConsole)).And.HaveCountGreaterThan(20);
        ConsoleModules.Descriptors.Should().NotContain(d => d.Id == "Switch");

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("MissingModule"), ServiceLifetime.Scoped);
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddLogging();
        ConsoleModules.Register(services);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        foreach (var console in ConsoleModules.ConsoleTypes)
            scope.ServiceProvider.GetRequiredService(typeof(ConsoleManager<>).MakeGenericType(console)).Should().NotBeNull();
        scope.ServiceProvider.GetServices<IConsoleOptions>().Should().NotContain(o => o is SwitchOptions);
        scope.ServiceProvider.GetServices<IDownloadableResources>().Should().NotContain(r => r is SwitchDownloads);
    }

    [Fact]
    public void CachedRowsOfAMissingModule_ReadAsPlainRoms()
    {
        var json = RomSnapshot.Serialize([new SwitchGame { TitleID = "0100000000010000", Path = "/a.nsp" }]);

        var rom = RomSnapshot.Deserialize(json).Should().ContainSingle().Subject;

        rom.GetType().Should().Be(typeof(Rom));
        rom.TitleID.Should().Be("0100000000010000");
    }

    [Fact]
    public void RowsWrittenBeforeTheSplit_StillResolveTheirType()
    {
        ConsoleModules.SetLoadedModules(_all);
        const string old = """[{"Type":"RomManagerShared.PS4.PS4Game, RomManagerShared","Data":{"TitleID":"CUSA00001"}}]""";

        RomSnapshot.Deserialize(old).Should().ContainSingle().Which.Should().BeOfType<RomManagerShared.PS4.PS4Game>();
    }
}

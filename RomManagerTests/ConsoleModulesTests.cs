using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using RomManagerShared.Base;
using RomManagerShared.Base.Library;
using RomManagerShared.Interfaces;

namespace RomManagerTests;

// A console defined outside RomManagerShared, the way a Modules/*.dll would.
public class TestModuleConsole : GamingConsole;

public class TestModuleGame : Game
{
    public string? Extra { get; set; }
}

public class TestModuleParser : IRomParser<TestModuleConsole>
{
    public List<string> Extensions { get; set; } = ["tmc"];
    public Task<List<Rom>> ProcessFile(string path) => Task.FromResult<List<Rom>>([new TestModuleGame { TitleID = "T1", Path = path }]);
}

public sealed class TestModuleMarker;

public class TestModule : IConsoleModule
{
    public ConsoleDescriptor Descriptor { get; } = new(typeof(TestModuleConsole), "TestModule", "Test Module Console");
    public void Register(IServiceCollection services) => services.AddSingleton<TestModuleMarker>();
}

[Collection("ConsoleModules")]
public sealed class ConsoleModulesTests : IDisposable
{
    public ConsoleModulesTests() => ConsoleModules.Use(typeof(TestModule).Assembly);
    public void Dispose() => ConsoleModules.Use();

    [Fact]
    public void Register_PicksUpConsolesParsersAndModuleServicesFromExtraAssemblies()
    {
        var services = new ServiceCollection();
        ConsoleModules.Register(services);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ConsoleManager<TestModuleConsole>>()
            .RomParserExecutor.GetSupportedExtensions().Should().Contain("tmc");
        scope.ServiceProvider.GetService<TestModuleMarker>().Should().NotBeNull();
        ConsoleModules.ConsoleTypes.Should().Contain(typeof(TestModuleConsole)).And.Contain(typeof(SwitchConsole));
    }

    [Fact]
    public void Describe_UsesModuleDescriptorOrKnownName()
    {
        ConsoleModules.Describe(typeof(TestModuleConsole)).DisplayName.Should().Be("Test Module Console");
        ConsoleModules.Describe(typeof(ThreeDSConsole)).DisplayName.Should().Be("Nintendo 3DS");
        ConsoleModules.Describe(typeof(NESConsole)).Id.Should().Be("NES");
    }

    [Fact]
    public void RomSnapshot_ResolvesTypesFromModuleAssemblies()
    {
        var json = RomSnapshot.Serialize([new TestModuleGame { TitleID = "T1", Extra = "x" }])
            .Replace(", " + typeof(TestModuleGame).Assembly.GetName().Name!, ", Unloadable.Name");

        RomSnapshot.Deserialize(json).Should().ContainSingle().Which.Should().BeOfType<TestModuleGame>()
            .Which.Extra.Should().Be("x");
    }
}

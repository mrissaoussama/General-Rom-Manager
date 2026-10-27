using RomManagerShared.Base;

[assembly: AssemblyFixture(typeof(RomManagerTests.ConsoleModulesFixture))]

namespace RomManagerTests;

/// <summary>The console modules are project references of the tests, so they load from the test folder.</summary>
public sealed class ConsoleModulesFixture
{
    public ConsoleModulesFixture() => ConsoleModules.Load("RomManager.Consoles.*.dll", AppContext.BaseDirectory);
}

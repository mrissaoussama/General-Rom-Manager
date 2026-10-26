using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using RomManagerShared.Base;
using RomManagerShared.Utils;

namespace RomManagerTests;

public sealed class PluginManagerTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("plugins").FullName;

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch (UnauthorizedAccessException) { /* plugin DLL may stay mapped */ }
    }

    private static string SamplePluginDll()
    {
        // tests run from RomManagerTests/bin/<Configuration>/net10.0
        var config = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(root, "SamplePlugin", "bin", config, "net10.0", "SamplePlugin.dll");
    }

    private PluginManager CreateManager(PluginApprovalStore store) =>
        new(new ServiceCollection().BuildServiceProvider(), NullLoggerFactory.Instance) { Approvals = store };

    private string CopySample()
    {
        var dest = Path.Combine(_dir, "Sample", "SamplePlugin.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.Copy(SamplePluginDll(), dest, overwrite: true);
        return dest;
    }

    [Fact]
    public async Task UnapprovedPlugin_IsNotLoaded_UnlessTheUserAllowsIt()
    {
        CopySample();
        var store = new PluginApprovalStore(Path.Combine(_dir, "approvals.json"));

        await using var first = CreateManager(store);
        await first.LoadPluginsAsync(_dir);
        first.Plugins.Should().BeEmpty();

        PluginApprovalRequest? asked = null;
        first.ApprovalCallback = r => { asked = r; return Task.FromResult(true); };
        await first.LoadPluginsAsync(_dir, askForApproval: true);

        first.Plugins.Should().ContainSingle().Which.PluginName.Should().Be("GameMetadataFetcher");
        asked!.Permissions.Should().BeEquivalentTo([PluginPermission.Network, PluginPermission.FileSystem]);

        // Reloading the same folder does not load the plugin twice.
        await first.LoadPluginsAsync(_dir, askForApproval: true);
        first.Plugins.Should().HaveCount(1);

        // The approval is remembered for the same DLL.
        await using var second = CreateManager(new PluginApprovalStore(store.Path));
        await second.LoadPluginsAsync(_dir);
        second.Plugins.Should().ContainSingle();
    }

    [Fact]
    public void ApprovalStore_RequiresApprovalAgainWhenTheDllOrPermissionsChange()
    {
        var store = new PluginApprovalStore(Path.Combine(_dir, "approvals.json"));
        var request = new PluginApprovalRequest("P", "1.0", null, null, [PluginPermission.Network], "p.dll", "AA");
        store.Approve(request);

        store.IsApproved(request).Should().BeTrue();
        store.IsApproved(request with { Sha256 = "BB" }).Should().BeFalse();
        store.IsApproved(request with { Permissions = [PluginPermission.Network, PluginPermission.ExternalProcess] }).Should().BeFalse();

        store.Revoke("P");
        new PluginApprovalStore(store.Path).IsApproved(request).Should().BeFalse();
    }
}

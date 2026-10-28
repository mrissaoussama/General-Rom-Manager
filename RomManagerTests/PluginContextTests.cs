using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using RomManagerShared.Base;
using RomManagerShared.Base.Database;
using RomManagerShared.Utils;

namespace RomManagerTests;

public sealed class PluginContextTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("plugin-data").FullName;

    public void Dispose() => Directory.Delete(_dir, true);

    private DefaultPluginContext Create(params PluginPermission[] granted)
    {
        var services = new ServiceCollection();
        // Never constructed for real: only whether the provider hands it out matters.
        services.AddSingleton(_ => (RomHashRepository)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(RomHashRepository)));
        return new DefaultPluginContext(services.BuildServiceProvider(), NullLogger.Instance, "Test", granted,
            _dir, Path.Combine(_dir, "config.json"), [], new FakePluginHost());
    }

    [Fact]
    public void Network_IsOnlyHandedOutWhenGranted()
    {
        using var denied = Create();
        denied.Invoking(c => c.CreateHttpClient()).Should().Throw<PluginPermissionException>()
            .Which.Permission.Should().Be(PluginPermission.Network);

        using var allowed = Create(PluginPermission.Network);
        using var client = allowed.CreateHttpClient();
        client.Should().NotBeNull();
    }

    [Fact]
    public void Files_InsideTheDataFolderAreAlwaysAllowed()
    {
        using var ctx = Create();
        using (var s = ctx.OpenFile("sub/notes.txt", FileMode.Create, FileAccess.Write)) s.WriteByte(1);
        File.Exists(Path.Combine(_dir, "sub", "notes.txt")).Should().BeTrue();

        var outside = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
        ctx.Invoking(c => c.OpenFile(outside, FileMode.Create, FileAccess.Write)).Should().Throw<PluginPermissionException>();
        ctx.Invoking(c => c.OpenFile("../escape.txt", FileMode.Create, FileAccess.Write)).Should().Throw<PluginPermissionException>();
        File.Exists(outside).Should().BeFalse();
    }

    [Fact]
    public void Files_OutsideTheDataFolderNeedFileSystem()
    {
        var outside = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
        try
        {
            using var ctx = Create(PluginPermission.FileSystem);
            using (var s = ctx.OpenFile(outside, FileMode.Create, FileAccess.Write)) s.WriteByte(1);
            File.Exists(outside).Should().BeTrue();
        }
        finally { File.Delete(outside); }
    }

    [Fact]
    public void Processes_NeedExternalProcess()
    {
        using var ctx = Create(PluginPermission.Network);
        ctx.Invoking(c => c.StartProcess(new System.Diagnostics.ProcessStartInfo("dotnet")))
            .Should().Throw<PluginPermissionException>();
    }

    [Fact]
    public void DatabaseServices_AreHiddenWithoutDatabase()
    {
        using var denied = Create();
        denied.Services.GetService<RomHashRepository>().Should().BeNull();

        using var allowed = Create(PluginPermission.Database);
        allowed.Services.GetService<RomHashRepository>().Should().NotBeNull();
    }
}

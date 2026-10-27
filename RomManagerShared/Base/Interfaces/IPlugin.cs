using Microsoft.Extensions.Logging;

namespace RomManagerShared.Base;

// ──────────────────────────────────────────────────────────────────────────────
// Plugin discovery attribute
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Decorate a class that implements <see cref="IPlugin"/> with this attribute
/// so that <see cref="RomManagerShared.Utils.PluginManager"/> can discover it.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PluginAttribute : Attribute
{
    public string Name { get; }
    public string Version { get; }
    public string? Description { get; init; }
    public string? Author { get; init; }
    /// <summary>
    /// Permissions shown to the user before any plugin code runs. <see cref="IPlugin.RequiredPermissions"/>
    /// may not ask for more than this.
    /// </summary>
    public PluginPermission[] Permissions { get; init; } = [];

    public PluginAttribute(string name, string version = "1.0.0")
    {
        Name = name;
        Version = version;
    }
}

/// <summary>The plugin contract version this build of RomManager loads.</summary>
public static class PluginApi
{
    public const int Version = 1;
}

/// <summary>
/// Declares which <see cref="PluginApi.Version"/> a plugin assembly was built against:
/// <c>[assembly: PluginApiVersion(PluginApi.Version)]</c>. Assemblies without it, or built for
/// another version, are not loaded.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class PluginApiVersionAttribute(int version) : Attribute
{
    public int Version { get; } = version;
}

// ──────────────────────────────────────────────────────────────────────────────
// Supporting types
// ──────────────────────────────────────────────────────────────────────────────

public enum PluginPermission
{
    /// <summary>Read/write files on disk.</summary>
    FileSystem,
    /// <summary>Make outbound HTTP requests.</summary>
    Network,
    /// <summary>Read and write the ROM database.</summary>
    Database,
    /// <summary>Launch external processes / tools.</summary>
    ExternalProcess
}

/// <summary>A single menu option exposed by a plugin.</summary>
public sealed class PluginOption
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    /// <summary>Avalonia resource key or file path for a 16×16 icon.</summary>
    public string? IconKey { get; init; }
    /// <summary>
    /// When set, the option only appears when the selected ROMs satisfy this predicate.
    /// </summary>
    public Func<IEnumerable<Rom>, bool>? CanExecutePredicate { get; init; }
}

// ──────────────────────────────────────────────────────────────────────────────
// Plugin context — services exposed to a plugin at runtime
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Scoped service bag passed to every plugin during initialisation and execution.
/// Plugins must only use this object to access app services — they must NOT take
/// a hard dependency on any concrete type from RomManagerShared.
/// <para>
/// Capabilities are handed out by permission: <see cref="CreateHttpClient"/> needs Network, files outside
/// the data folder need FileSystem, <see cref="StartProcess"/> needs ExternalProcess, and database services
/// from <see cref="Services"/> need Database. Plugins run in-process, so this is not a sandbox: the
/// approval prompt is the trust boundary, and these checks keep a plugin to what it declared.
/// </para>
/// </summary>
public interface IPluginContext
{
    /// <summary>DI services scoped to this plugin's lifetime.</summary>
    IServiceProvider Services { get; }

    /// <summary>Logger pre-scoped with the plugin name as the category.</summary>
    ILogger Logger { get; }

    /// <summary>
    /// Writable folder for this plugin's persistent data.
    /// Guaranteed to exist when the context is created.
    /// </summary>
    string PluginDataDirectory { get; }

    /// <summary>Path to the plugin's JSON config file (may not exist yet).</summary>
    string ConfigFilePath { get; }

    /// <summary>
    /// Allows a plugin to display arbitrary UI (Avalonia Control)
    /// inside the host application's plugin panel.
    /// </summary>
    Task ShowPluginUIAsync(object avaloniaControl, string title);

    /// <summary>Read-only access to the current selection in the ROM list.</summary>
    IReadOnlyList<Rom> SelectedRoms { get; }

    /// <summary>The permissions the user approved for this plugin.</summary>
    IReadOnlySet<PluginPermission> GrantedPermissions { get; }

    /// <summary>An <see cref="HttpClient"/> for the plugin. Requires <see cref="PluginPermission.Network"/>.</summary>
    /// <exception cref="PluginPermissionException">Network was not approved.</exception>
    HttpClient CreateHttpClient();

    /// <summary>
    /// Opens a file. Paths inside <see cref="PluginDataDirectory"/> are always allowed (relative paths resolve
    /// there); anything else requires <see cref="PluginPermission.FileSystem"/>.
    /// </summary>
    /// <exception cref="PluginPermissionException">The path is outside the data folder and FileSystem was not approved.</exception>
    Stream OpenFile(string path, FileMode mode, FileAccess access);

    /// <summary>Starts an external program. Requires <see cref="PluginPermission.ExternalProcess"/>.</summary>
    /// <exception cref="PluginPermissionException">ExternalProcess was not approved.</exception>
    System.Diagnostics.Process? StartProcess(System.Diagnostics.ProcessStartInfo startInfo);
}

/// <summary>Thrown when a plugin uses a capability the user did not approve.</summary>
public sealed class PluginPermissionException(string pluginName, PluginPermission permission)
    : InvalidOperationException($"Plugin '{pluginName}' needs the {permission} permission for this.")
{
    public PluginPermission Permission { get; } = permission;
}

// ──────────────────────────────────────────────────────────────────────────────
// Core plugin interface
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// All externally-loaded plugins must implement this interface.
/// </summary>
public interface IPlugin : IAsyncDisposable
{
    // ── Identity ───────────────────────────────────────────────────────────
    string PluginName { get; }
    string PluginVersion { get; }
    string PluginDescription { get; }
    string PluginAuthor { get; }

    // ── Capabilities ───────────────────────────────────────────────────────
    /// <summary>
    /// Console names this plugin applies to (e.g. "Switch", "PS4").
    /// Return an empty list to make the plugin globally available.
    /// </summary>
    IReadOnlyList<string> SupportedConsoles { get; }

    /// <summary>Permissions the plugin declares it will use.</summary>
    IReadOnlyList<PluginPermission> RequiredPermissions { get; }

    /// <summary>Options that appear in the plugin's context/option menu.</summary>
    IReadOnlyList<PluginOption> Options { get; }

    // ── Lifecycle ──────────────────────────────────────────────────────────
    /// <summary>Called once after the plugin assembly is loaded.</summary>
    Task InitializeAsync(IPluginContext context);

    /// <summary>
    /// Execute the option at <paramref name="optionIndex"/> from <see cref="Options"/>.
    /// </summary>
    Task ExecuteAsync(int optionIndex, IPluginContext context);
}

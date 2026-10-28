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
    ExternalProcess,
    /// <summary>Change titles, descriptions, covers and other metadata of roms in the library.</summary>
    LibraryMetadata,
}

/// <summary>A single menu option exposed by a plugin.</summary>
public sealed class PluginOption
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    /// <summary>
    /// A 16×16 icon: an image file (a relative path is resolved next to the plugin's DLL) or an Avalonia resource key.
    /// </summary>
    public string? IconKey { get; init; }

    /// <summary>Submenu of the rom context menu the option appears in; the plugin's name when null.</summary>
    public string? Category { get; init; }
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
    /// Shows an Avalonia <c>Control</c> (or a string) in a window. Prefer <see cref="ShowMessageAsync"/> and
    /// <see cref="ShowTableAsync"/>, which need no Avalonia reference and match the app's look.
    /// </summary>
    Task ShowPluginUIAsync(object avaloniaControl, string title);

    /// <summary>
    /// Copies of the roms selected when the option ran (empty in hooks). Changing them changes nothing in the
    /// library; use <see cref="UpdateMetadataAsync"/>.
    /// </summary>
    IReadOnlyList<Rom> SelectedRoms { get; }

    /// <summary>The console page the option ran from ("Switch", "PS2"); empty in hooks.</summary>
    string ConsoleId { get; }

    /// <summary>That console's display name ("Nintendo Switch"); empty in hooks.</summary>
    string ConsoleName { get; }

    /// <summary>Progress of the running option, 0–100; shown in the Operations panel.</summary>
    IProgress<double> Progress { get; }

    /// <summary>
    /// Saves metadata for a rom (one of <see cref="SelectedRoms"/>, or any rom the plugin was given). The values
    /// are kept apart from scan results, so rescans and moves keep them, and the user can undo the option's changes
    /// from the Operations panel. Requires <see cref="PluginPermission.LibraryMetadata"/>.
    /// </summary>
    /// <exception cref="PluginPermissionException">LibraryMetadata was not approved.</exception>
    Task UpdateMetadataAsync(Rom rom, RomMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>Shows a message with a Close button.</summary>
    Task ShowMessageAsync(string title, string message);

    /// <summary>Shows rows of text under column headers (e.g. lookup results).</summary>
    Task ShowTableAsync(string title, IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<string>> rows);

    /// <summary>Asks a yes/no question; true when the user confirmed.</summary>
    Task<bool> ConfirmAsync(string title, string message, string? confirmText = null);

    /// <summary>
    /// Lets the user choose where to save a file; null when cancelled. The chosen file can then be written with
    /// <see cref="OpenFile"/> without the FileSystem permission.
    /// </summary>
    Task<string?> PickSaveFileAsync(string title, string suggestedName, string extension);

    /// <summary>Opens an http(s) address in the user's browser.</summary>
    /// <exception cref="ArgumentException">The address is not http or https.</exception>
    void OpenInBrowser(Uri url);

    /// <summary>The permissions the user approved for this plugin.</summary>
    IReadOnlySet<PluginPermission> GrantedPermissions { get; }

    /// <summary>
    /// The values of the plugin's <see cref="IPluginSettings.SettingDescriptors"/>, as the user set them in
    /// Settings → Plugins (defaults where unset). Empty for plugins without settings.
    /// </summary>
    IReadOnlyDictionary<string, string> Settings => new Dictionary<string, string>();

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
// Settings
// ──────────────────────────────────────────────────────────────────────────────

public enum PluginSettingKind
{
    Text,
    /// <summary>Hidden while typing (API keys, passwords). Stored like Text, in the plugin's data folder.</summary>
    Secret,
    Boolean,
}

/// <summary>One option a plugin offers in Settings → Plugins.</summary>
public sealed record PluginSettingDescriptor(string Key, string Label, PluginSettingKind Kind = PluginSettingKind.Text,
    string Default = "", string? Description = null);

/// <summary>Optional for plugins: implement next to <see cref="IPlugin"/> to get options edited by the app.</summary>
public interface IPluginSettings
{
    IReadOnlyList<PluginSettingDescriptor> SettingDescriptors { get; }
}

// ──────────────────────────────────────────────────────────────────────────────
// Library hooks
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>A finished scan of one console's library. Roms are keyed by path, type and title ID.</summary>
public sealed record LibraryScanCompleted(string Console, IReadOnlyList<Rom> Roms, IReadOnlyList<Rom> Added, IReadOnlyList<Rom> Removed)
{
    /// <summary>Compares the library before and after a scan.</summary>
    public static LibraryScanCompleted Diff(string console, IEnumerable<Rom> before, IReadOnlyList<Rom> after)
    {
        static string Key(Rom r) => $"{r.Path?.ToUpperInvariant()}|{r.GetType().Name}|{r.TitleID}";
        var old = before.ToLookup(Key);
        var now = after.Select(Key).ToHashSet();
        return new LibraryScanCompleted(console, after,
            [.. after.Where(r => !old.Contains(Key(r)))],
            [.. old.Where(g => !now.Contains(g.Key)).SelectMany(g => g)]);
    }
}

/// <summary>
/// Optional for plugins: implement next to <see cref="IPlugin"/> to hear about library changes. Called for
/// the consoles in <see cref="IPlugin.SupportedConsoles"/> (all when empty), off the UI thread.
/// </summary>
public interface ILibraryHooks
{
    /// <summary>A scan of <see cref="LibraryScanCompleted.Console"/> finished; Added/Removed are relative to the
    /// library shown before it.</summary>
    Task OnScanCompletedAsync(LibraryScanCompleted scan, IPluginContext context);

    /// <summary>Files of the library were renamed, moved (also by an undo) or deleted from the app.</summary>
    Task OnFilesChangedAsync(LibraryFilesChanged change, IPluginContext context) => Task.CompletedTask;
}

public enum LibraryFileChangeKind
{
    Renamed,
    Moved,
    Deleted,
}

/// <summary>One file or folder: where it was and where it is now (null when deleted).</summary>
public sealed record LibraryFileChange(string OldPath, string? NewPath);

/// <summary>Files changed by one operation of the app. <see cref="Console"/> is the console id ("Switch"), or "" when mixed.</summary>
public sealed record LibraryFilesChanged(string Console, LibraryFileChangeKind Kind, IReadOnlyList<LibraryFileChange> Files);

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
    /// Execute the option at <paramref name="optionIndex"/> from <see cref="Options"/>. Runs in the app's operation
    /// queue: <paramref name="cancellationToken"/> fires when the user cancels it.
    /// </summary>
    Task ExecuteAsync(int optionIndex, IPluginContext context, CancellationToken cancellationToken);
}

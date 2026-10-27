using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RomManagerShared.Base;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Loader;

namespace RomManagerShared.Utils;

// ──────────────────────────────────────────────────────────────────────────────
// Isolated load context — one per plugin DLL
// ──────────────────────────────────────────────────────────────────────────────

internal sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path is not null ? LoadFromAssemblyPath(path) : null;
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path is not null ? LoadUnmanagedDllFromPath(path) : base.LoadUnmanagedDll(unmanagedDllName);
    }
}

// ──────────────────────────────────────────────────────────────────────────────
// Default IPluginContext implementation
// ──────────────────────────────────────────────────────────────────────────────

internal sealed class DefaultPluginContext : IPluginContext, IDisposable
{
    private readonly IServiceScope _scope;
    private readonly Func<object, string, Task> _showUI;
    private readonly string _pluginName;

    public DefaultPluginContext(
        IServiceProvider rootProvider,
        ILogger logger,
        string pluginName,
        IEnumerable<PluginPermission> granted,
        string pluginDataDirectory,
        string configFilePath,
        IReadOnlyList<Rom> selectedRoms,
        Func<object, string, Task> showUI)
    {
        _scope = rootProvider.CreateScope();
        _pluginName = pluginName;
        GrantedPermissions = granted.ToHashSet();
        Services = new PermissionFilteredServiceProvider(_scope.ServiceProvider, GrantedPermissions);
        Logger = logger;
        PluginDataDirectory = pluginDataDirectory;
        ConfigFilePath = configFilePath;
        SelectedRoms = selectedRoms;
        _showUI = showUI;
    }

    public IServiceProvider Services { get; }
    public ILogger Logger { get; }
    public string PluginDataDirectory { get; }
    public string ConfigFilePath { get; }
    public IReadOnlyList<Rom> SelectedRoms { get; }
    public IReadOnlySet<PluginPermission> GrantedPermissions { get; }

    public Task ShowPluginUIAsync(object avaloniaControl, string title)
        => _showUI(avaloniaControl, title);

    private void Demand(PluginPermission permission)
    {
        if (!GrantedPermissions.Contains(permission))
            throw new PluginPermissionException(_pluginName, permission);
    }

    public HttpClient CreateHttpClient()
    {
        Demand(PluginPermission.Network);
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"RomManager-Plugin/{PluginApi.Version}");
        return client;
    }

    public Stream OpenFile(string path, FileMode mode, FileAccess access)
    {
        var full = Path.GetFullPath(path, PluginDataDirectory);
        if (!IsInside(full, PluginDataDirectory)) Demand(PluginPermission.FileSystem);
        if (access != FileAccess.Read) Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        return new FileStream(full, mode, access, access == FileAccess.Read ? FileShare.Read : FileShare.None);
    }

    public System.Diagnostics.Process? StartProcess(System.Diagnostics.ProcessStartInfo startInfo)
    {
        Demand(PluginPermission.ExternalProcess);
        return System.Diagnostics.Process.Start(startInfo);
    }

    internal static bool IsInside(string fullPath, string folder)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder)) + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    public void Dispose() => _scope.Dispose();
}

/// <summary>
/// Hides services a plugin has no permission for: database types (EF contexts and
/// <c>RomManagerShared.Base.Database</c>) need Database, HTTP types need Network.
/// </summary>
internal sealed class PermissionFilteredServiceProvider(IServiceProvider inner, IReadOnlySet<PluginPermission> granted)
    : IServiceProvider
{
    public object? GetService(Type serviceType) =>
        RequiredPermission(serviceType) is { } needed && !granted.Contains(needed) ? null : inner.GetService(serviceType);

    internal static PluginPermission? RequiredPermission(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            type = type.GetGenericArguments()[0];
        if (typeof(Microsoft.EntityFrameworkCore.DbContext).IsAssignableFrom(type)
            || type.Namespace?.StartsWith("RomManagerShared.Base.Database", StringComparison.Ordinal) == true
            || type.Namespace?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true)
            return PluginPermission.Database;
        if (typeof(HttpClient).IsAssignableFrom(type) || type.Namespace == "System.Net.Http")
            return PluginPermission.Network;
        return null;
    }
}

// ──────────────────────────────────────────────────────────────────────────────
// PluginEntry — pairs a plugin instance with its isolated load context
// ──────────────────────────────────────────────────────────────────────────────

public sealed record PluginEntry(IPlugin Plugin, WeakReference<AssemblyLoadContext> LoadContext);

// ──────────────────────────────────────────────────────────────────────────────
// PluginManager
// ──────────────────────────────────────────────────────────────────────────────

public sealed class PluginManager : IAsyncDisposable
{
    private readonly List<PluginEntry> _entries = [];
    private readonly HashSet<string> _loadedPaths = new(StringComparer.OrdinalIgnoreCase);
    // Startup loads plugins in the background while Settings can import or reload; one load at a time.
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private PluginApprovalStore? _approvals;
    private readonly IServiceProvider _rootServices;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<PluginManager> _logger;

    /// <summary>
    /// Delegate the host application must supply to allow plugins to push UI into
    /// the shell (e.g. open a flyout / dialog containing the plugin's Avalonia control).
    /// </summary>
    public Func<object, string, Task>? ShowPluginUICallback { get; set; }

    /// <summary>
    /// Asks the user to approve a plugin that is new, changed, or asks for different permissions.
    /// Only consulted when loading with <c>askForApproval</c>; otherwise unapproved plugins are skipped.
    /// </summary>
    public Func<PluginApprovalRequest, Task<bool>>? ApprovalCallback { get; set; }

    public PluginApprovalStore Approvals
    {
        get => _approvals ??= new PluginApprovalStore(Path.Combine(
            RomManagerShared.Configuration.RomManagerConfiguration.GetGlobalPath("Plugins"), "approvals.json"));
        set => _approvals = value;
    }

    /// <summary>Returns a snapshot of currently loaded plugins.</summary>
    public IReadOnlyList<IPlugin> Plugins
    {
        get { lock (_entries) return _entries.Select(e => e.Plugin).ToList(); }
    }

    public PluginManager(IServiceProvider rootServices, ILoggerFactory loggerFactory)
    {
        _rootServices = rootServices;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<PluginManager>();
    }

    // ── Loading ───────────────────────────────────────────────────────────

    /// <summary>
    /// Scans <paramref name="pluginDirectory"/> for DLL files and loads any class
    /// decorated with <see cref="PluginAttribute"/> that implements <see cref="IPlugin"/>.
    /// Each DLL is loaded into an isolated <see cref="AssemblyLoadContext"/>. Sub-folders are scanned too
    /// (imported zips extract into <c>Plugins/&lt;name&gt;/</c>); DLLs already loaded are skipped.
    /// </summary>
    public async Task LoadPluginsAsync(string pluginDirectory, IReadOnlyList<Rom>? selectedRoms = null,
        bool askForApproval = false)
    {
        if (!Directory.Exists(pluginDirectory))
        {
            // Auto-create the directory so subsequent runs find it.
            try { Directory.CreateDirectory(pluginDirectory); }
            catch { /* ignore permission errors */ }
            return;
        }

        var dlls = Directory.EnumerateFiles(pluginDirectory, "*.dll", SearchOption.AllDirectories).ToList();

        await _loadGate.WaitAsync();
        try
        {
            foreach (var dll in dlls)
            {
                if (_loadedPaths.Contains(Path.GetFullPath(dll))) continue;
                await TryLoadPluginDllAsync(dll, selectedRoms ?? [], askForApproval);
            }
        }
        finally
        {
            _loadGate.Release();
        }
    }

    private async Task TryLoadPluginDllAsync(string dllPath, IReadOnlyList<Rom> selectedRoms, bool askForApproval)
    {
        var loadContext = new PluginLoadContext(dllPath);
        try
        {
            var assembly = loadContext.LoadFromAssemblyPath(dllPath);

            // Scan for types decorated with [Plugin] that implement IPlugin
            var pluginTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract
                         && !t.IsInterface
                         && typeof(IPlugin).IsAssignableFrom(t)
                         && t.GetCustomAttribute<PluginAttribute>() is not null)
                .ToList();

            if (pluginTypes.Count == 0)
            {
                _logger.LogDebug("No [Plugin] types found in {DLL}", Path.GetFileName(dllPath));
                loadContext.Unload();
                return;
            }

            var apiVersion = assembly.GetCustomAttribute<PluginApiVersionAttribute>()?.Version;
            if (apiVersion != PluginApi.Version)
            {
                _logger.LogWarning("Skipped plugin {DLL}: built for plugin API {Found}, this app loads {Expected}. Add [assembly: PluginApiVersion({Expected})] and rebuild.",
                    Path.GetFileName(dllPath), apiVersion?.ToString() ?? "(none)", PluginApi.Version, PluginApi.Version);
                loadContext.Unload();
                return;
            }

            var sha256 = PluginApprovalRequest.HashFile(dllPath);
            int loaded = 0;
            foreach (var type in pluginTypes)
            {
                var attr = type.GetCustomAttribute<PluginAttribute>()!;
                var request = new PluginApprovalRequest(attr.Name, attr.Version, attr.Author, attr.Description,
                    attr.Permissions, dllPath, sha256);
                if (!await IsApprovedAsync(request, askForApproval)) continue;
                if (await TryInstantiateAndInitAsync(type, loadContext, dllPath, selectedRoms)) loaded++;
            }
            if (loaded == 0) loadContext.Unload();
            else _loadedPaths.Add(Path.GetFullPath(dllPath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin DLL: {DLL}", dllPath);
            loadContext.Unload();
        }
    }

    private async Task<bool> IsApprovedAsync(PluginApprovalRequest request, bool askForApproval)
    {
        if (Approvals.IsApproved(request)) return true;
        if (askForApproval && ApprovalCallback is not null && await ApprovalCallback(request))
        {
            Approvals.Approve(request);
            return true;
        }
        _logger.LogWarning("Plugin {Name} {Version} is not approved; approve it from Settings → Plugins → Reload.",
            request.Name, request.Version);
        return false;
    }

    private async Task<bool> TryInstantiateAndInitAsync(
        Type type,
        PluginLoadContext loadContext,
        string dllPath,
        IReadOnlyList<Rom> selectedRoms)
    {
        try
        {
            var attr = type.GetCustomAttribute<PluginAttribute>()!;
            var plugin = (IPlugin)Activator.CreateInstance(type)!;
            var undeclared = plugin.RequiredPermissions.Except(attr.Permissions).ToList();
            if (undeclared.Count > 0)
            {
                _logger.LogWarning("Plugin {Name} asks for {Permissions} at runtime but does not declare them in [Plugin(Permissions = ...)]; not loaded.",
                    attr.Name, string.Join(", ", undeclared));
                await plugin.DisposeAsync();
                return false;
            }

            // Build a plugin-scoped data directory
            var pluginDataDir = Path.Combine(
                RomManagerShared.Configuration.RomManagerConfiguration.GetBaseFolderPath(),
                "Plugins",
                attr.Name);
            try { Directory.CreateDirectory(pluginDataDir); }
            catch { /* best-effort; if the data dir can't be created the plugin may still function without it */ }

            var configFile = Path.Combine(pluginDataDir, "config.json");
            var pluginLogger = _loggerFactory.CreateLogger(type.FullName ?? attr.Name);

            var context = new DefaultPluginContext(
                _rootServices,
                pluginLogger,
                attr.Name,
                attr.Permissions,
                pluginDataDir,
                configFile,
                selectedRoms,
                ShowPluginUICallback ?? ((_, _) => Task.CompletedTask));

            await plugin.InitializeAsync(context);

            lock (_entries) _entries.Add(new PluginEntry(plugin, new WeakReference<AssemblyLoadContext>(loadContext)));
            _logger.LogInformation("Loaded plugin: {Name} v{Version}", plugin.PluginName, plugin.PluginVersion);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialise plugin type: {Type}", type.FullName);
            return false;
        }
    }

    // ── Execution ──────────────────────────────────────────────────────────

    /// <summary>
    /// Executes the option at <paramref name="optionIndex"/> on the plugin identified
    /// by <paramref name="pluginName"/>.
    /// </summary>
    public async Task ExecuteAsync(string pluginName, int optionIndex, IReadOnlyList<Rom>? selectedRoms = null)
    {
        PluginEntry? found;
        lock (_entries) found = _entries.FirstOrDefault(e => e.Plugin.PluginName == pluginName);
        var entry = found
            ?? throw new InvalidOperationException($"Plugin '{pluginName}' is not loaded.");

        using var context = CreateContext(entry.Plugin, selectedRoms ?? []);
        await entry.Plugin.ExecuteAsync(optionIndex, context);
    }

    /// <summary>
    /// Tells every loaded <see cref="ILibraryHooks"/> plugin for the console that a scan finished. Each plugin
    /// runs in turn; one that throws is logged and does not stop the others.
    /// </summary>
    /// <param name="isEnabled">Skips plugins the user switched off.</param>
    public async Task NotifyScanCompletedAsync(LibraryScanCompleted scan, Func<IPlugin, bool>? isEnabled = null)
    {
        foreach (var plugin in GetPluginsForConsole(scan.Console).OfType<ILibraryHooks>().ToList())
        {
            var p = (IPlugin)plugin;
            if (isEnabled is not null && !isEnabled(p)) continue;
            try
            {
                using var context = CreateContext(p, []);
                await plugin.OnScanCompletedAsync(scan, context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Plugin {Name} failed in OnScanCompletedAsync", p.PluginName);
            }
        }
    }

    private DefaultPluginContext CreateContext(IPlugin plugin, IReadOnlyList<Rom> selectedRoms)
    {
        var attr = plugin.GetType().GetCustomAttribute<PluginAttribute>();
        var name = attr?.Name ?? plugin.PluginName;
        var pluginDataDir = Path.Combine(RomManagerShared.Configuration.RomManagerConfiguration.GetBaseFolderPath(), "Plugins", name);
        return new DefaultPluginContext(
            _rootServices,
            _loggerFactory.CreateLogger(plugin.GetType().FullName ?? name),
            name,
            attr?.Permissions ?? [],
            pluginDataDir,
            Path.Combine(pluginDataDir, "config.json"),
            selectedRoms,
            ShowPluginUICallback ?? ((_, _) => Task.CompletedTask));
    }

    // ── Query helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Imports a plugin packed as a ZIP file.
    /// The archive is extracted to <c>plugins/&lt;zipNameWithoutExtension&gt;/</c> and 
    /// then loaded immediately.
    /// </summary>
    public async Task<string> ImportZipAsync(string zipPath, bool askForApproval = true)
    {
        if (!File.Exists(zipPath))
            throw new FileNotFoundException($"Plugin zip not found: {zipPath}");

        var pluginsDir = RomManagerShared.Configuration.RomManagerConfiguration.GetGlobalPath("Plugins");
        var pluginName = Path.GetFileNameWithoutExtension(zipPath);
        var destDir    = Path.Combine(pluginsDir, pluginName);

        // Overwrite any existing version
        if (Directory.Exists(destDir))
            Directory.Delete(destDir, recursive: true);

        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, destDir);

        _logger.LogInformation("Imported plugin '{Name}' from {Zip}", pluginName, zipPath);

        // Load newly extracted DLLs
        await LoadPluginsAsync(destDir, askForApproval: askForApproval);

        return pluginName;
    }

    /// <summary>Returns plugins that apply to a given console name.</summary>
    public IEnumerable<IPlugin> GetPluginsForConsole(string consoleName)
        => Plugins
            .Where(p => p.SupportedConsoles.Count == 0
                     || p.SupportedConsoles.Contains(consoleName, StringComparer.OrdinalIgnoreCase));

    // ── Disposal ───────────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        List<PluginEntry> entries;
        lock (_entries)
        {
            entries = [.. _entries];
            _entries.Clear();
        }
        foreach (var entry in entries)
        {
            try { await entry.Plugin.DisposeAsync(); }
            catch { /* best-effort */ }

            if (entry.LoadContext.TryGetTarget(out var ctx))
                ctx.Unload();
        }
    }
}


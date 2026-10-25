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

internal sealed class DefaultPluginContext : IPluginContext
{
    private readonly IServiceScope _scope;
    private readonly Func<object, string, Task> _showUI;

    public DefaultPluginContext(
        IServiceProvider rootProvider,
        ILogger logger,
        string pluginDataDirectory,
        string configFilePath,
        IReadOnlyList<Rom> selectedRoms,
        Func<object, string, Task> showUI)
    {
        _scope = rootProvider.CreateScope();
        Services = _scope.ServiceProvider;
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

    public Task ShowPluginUIAsync(object avaloniaControl, string title)
        => _showUI(avaloniaControl, title);

    public void Dispose() => _scope.Dispose();
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
    private readonly IServiceProvider _rootServices;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<PluginManager> _logger;

    /// <summary>
    /// Delegate the host application must supply to allow plugins to push UI into
    /// the shell (e.g. open a flyout / dialog containing the plugin's Avalonia control).
    /// </summary>
    public Func<object, string, Task>? ShowPluginUICallback { get; set; }

    /// <summary>Returns a snapshot of currently loaded plugins.</summary>
    public IReadOnlyList<IPlugin> Plugins => _entries.Select(e => e.Plugin).ToList();

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
    /// Each DLL is loaded into an isolated <see cref="AssemblyLoadContext"/>.
    /// </summary>
    public async Task LoadPluginsAsync(string pluginDirectory, IReadOnlyList<Rom>? selectedRoms = null)
    {
        if (!Directory.Exists(pluginDirectory))
        {
            // Auto-create the directory so subsequent runs find it.
            try { Directory.CreateDirectory(pluginDirectory); }
            catch { /* ignore permission errors */ }
            return;
        }

        var dlls = Directory.EnumerateFiles(pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly);

        foreach (var dll in dlls)
        {
            await TryLoadPluginDllAsync(dll, selectedRoms ?? []);
        }
    }

    private async Task TryLoadPluginDllAsync(string dllPath, IReadOnlyList<Rom> selectedRoms)
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

            foreach (var type in pluginTypes)
            {
                await TryInstantiateAndInitAsync(type, loadContext, dllPath, selectedRoms);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin DLL: {DLL}", dllPath);
            loadContext.Unload();
        }
    }

    private async Task TryInstantiateAndInitAsync(
        Type type,
        PluginLoadContext loadContext,
        string dllPath,
        IReadOnlyList<Rom> selectedRoms)
    {
        try
        {
            var attr = type.GetCustomAttribute<PluginAttribute>()!;
            var plugin = (IPlugin)Activator.CreateInstance(type)!;

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
                pluginDataDir,
                configFile,
                selectedRoms,
                ShowPluginUICallback ?? ((_, _) => Task.CompletedTask));

            await plugin.InitializeAsync(context);

            _entries.Add(new PluginEntry(plugin, new WeakReference<AssemblyLoadContext>(loadContext)));
            _logger.LogInformation("Loaded plugin: {Name} v{Version}", plugin.PluginName, plugin.PluginVersion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialise plugin type: {Type}", type.FullName);
        }
    }

    // ── Execution ──────────────────────────────────────────────────────────

    /// <summary>
    /// Executes the option at <paramref name="optionIndex"/> on the plugin identified
    /// by <paramref name="pluginName"/>.
    /// </summary>
    public async Task ExecuteAsync(string pluginName, int optionIndex, IReadOnlyList<Rom>? selectedRoms = null)
    {
        var entry = _entries.FirstOrDefault(e => e.Plugin.PluginName == pluginName)
            ?? throw new InvalidOperationException($"Plugin '{pluginName}' is not loaded.");

        var pluginLogger = _loggerFactory.CreateLogger(entry.Plugin.GetType().FullName ?? pluginName);

        var pluginDataDir = Path.Combine(
            RomManagerShared.Configuration.RomManagerConfiguration.GetBaseFolderPath(),
            "Plugins",
            pluginName);

        var context = new DefaultPluginContext(
            _rootServices,
            pluginLogger,
            pluginDataDir,
            Path.Combine(pluginDataDir, "config.json"),
            selectedRoms ?? [],
            ShowPluginUICallback ?? ((_, _) => Task.CompletedTask));

        await entry.Plugin.ExecuteAsync(optionIndex, context);
    }

    // ── Query helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Imports a plugin packed as a ZIP file.
    /// The archive is extracted to <c>plugins/&lt;zipNameWithoutExtension&gt;/</c> and 
    /// then loaded immediately.
    /// </summary>
    public async Task<string> ImportZipAsync(string zipPath)
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
        await LoadPluginsAsync(destDir);

        return pluginName;
    }

    /// <summary>Returns plugins that apply to a given console name.</summary>
    public IEnumerable<IPlugin> GetPluginsForConsole(string consoleName)
        => _entries
            .Select(e => e.Plugin)
            .Where(p => p.SupportedConsoles.Count == 0
                     || p.SupportedConsoles.Contains(consoleName, StringComparer.OrdinalIgnoreCase));

    // ── Disposal ───────────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        foreach (var entry in _entries)
        {
            try { await entry.Plugin.DisposeAsync(); }
            catch { /* best-effort */ }

            if (entry.LoadContext.TryGetTarget(out var ctx))
                ctx.Unload();
        }

        _entries.Clear();
    }
}


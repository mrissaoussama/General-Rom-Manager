using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using RomManagerShared.Base.Database;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;

namespace RomManagerShared.Base;

/// <param name="Id">Stable key derived from the type name (config sections, settings, library cache).</param>
/// <param name="Manufacturer">Used to colour the console badge; empty when unknown.</param>
/// <param name="Badge">Two to four characters shown as the console's icon.</param>
public sealed record ConsoleDescriptor(Type ConsoleType, string Id, string DisplayName, string Manufacturer = "", string Badge = "");

/// <summary>
/// Optional hook for a console assembly (ADR 0003). Parsers, providers, managers and organizers are
/// registered by convention anyway; implement this only to register extra services or rename the console.
/// </summary>
public interface IConsoleModule
{
    ConsoleDescriptor Descriptor { get; }
    void Register(IServiceCollection services);
}

/// <summary>
/// Finds consoles in the console module assemblies (<c>Consoles/*.dll</c>) and registers their services.
/// Module assemblies load into the default context so they share <see cref="Rom"/>, EF and DI types.
/// </summary>
public static class ConsoleModules
{
    // The core library and the rom model (RomManager.Abstractions); consoles come from module assemblies.
    private static readonly Assembly[] CoreAssemblies = [typeof(GamingConsole).Assembly, typeof(Rom).Assembly];
    private static List<Assembly> _loaded = [];
    private static List<Assembly> _assemblies = [.. CoreAssemblies];

    public static IReadOnlyList<Assembly> Assemblies => _assemblies;

    /// <summary>Console modules shipped with the app: <c>&lt;app folder&gt;/Consoles</c>.</summary>
    public static string AppModulesDirectory => Path.Combine(AppContext.BaseDirectory, "Consoles");

    /// <summary>Extra modules the user adds (config <c>GlobalPaths:Modules</c>, default <c>&lt;data folder&gt;/Consoles</c>).</summary>
    public static string ModulesDirectory =>
        Configuration.RomManagerConfiguration.GetPathOrDefault("GlobalPaths:Modules", "Consoles");

    /// <summary>
    /// Loads console modules (assemblies that define a <see cref="GamingConsole"/>) from the app's and the user's
    /// Consoles folders, or from <paramref name="directories"/>. A module that fails to load is logged and skipped;
    /// the app works with whatever consoles remain.
    /// </summary>
    public static IReadOnlyList<Assembly> Load(params string[] directories) => Load("*.dll", directories);

    public static IReadOnlyList<Assembly> Load(string pattern, params string[] directories)
    {
        if (directories.Length == 0) directories = [AppModulesDirectory, ModulesDirectory];
        var loaded = new List<Assembly>();
        foreach (var directory in directories.Distinct(FileUtils.PathComparer).Where(Directory.Exists))
        {
            foreach (var dll in Directory.EnumerateFiles(directory, pattern))
            {
                try
                {
                    var asm = Assembly.LoadFrom(dll);
                    if (!loaded.Contains(asm) && SafeTypes(asm).Any(t => typeof(GamingConsole).IsAssignableFrom(t) && t != typeof(GamingConsole)))
                        loaded.Add(asm);
                }
                catch (Exception ex) { AppLog.Warn($"[Modules] {Path.GetFileName(dll)}: {ex.Message}"); }
            }
        }
        _loaded = loaded;
        _assemblies = [.. CoreAssemblies, .. loaded];
        return _assemblies;
    }

    /// <summary>Replaces the loaded modules (tests: simulate a module DLL that is missing).</summary>
    internal static void SetLoadedModules(IEnumerable<Assembly> modules)
    {
        _loaded = [.. modules];
        _assemblies = [.. CoreAssemblies, .. _loaded];
    }

    /// <summary>The loaded modules plus extra assemblies (tests, harness) without touching the file system.</summary>
    public static void Use(params Assembly[] assemblies) =>
        _assemblies = [.. CoreAssemblies.Concat(_loaded).Concat(assemblies).Distinct()];

    public static IEnumerable<Type> AllTypes => _assemblies.SelectMany(SafeTypes);

    public static IReadOnlyList<Type> ConsoleTypes =>
        [.. AllTypes.Where(t => typeof(GamingConsole).IsAssignableFrom(t) && !t.IsAbstract && t != typeof(GamingConsole))];

    public static IReadOnlyList<IConsoleModule> Modules =>
        [.. AllTypes.Where(t => typeof(IConsoleModule).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) is not null)
            .Select(t => (IConsoleModule)Activator.CreateInstance(t)!)];

    public static ConsoleDescriptor Describe(Type consoleType)
    {
        var module = Modules.FirstOrDefault(m => m.Descriptor.ConsoleType == consoleType);
        if (module is not null) return module.Descriptor;
        var id = consoleType.Name.Replace("Console", "");
        return consoleType.GetCustomAttribute<ConsoleInfoAttribute>() is { } info
            ? new ConsoleDescriptor(consoleType, id, info.DisplayName, info.Manufacturer, info.Badge)
            : new ConsoleDescriptor(consoleType, id, id, "", id.Length <= 4 ? id : id[..3]);
    }

    /// <summary>Every console, sorted by display name.</summary>
    public static IReadOnlyList<ConsoleDescriptor> Descriptors =>
        [.. ConsoleTypes.Select(Describe).OrderBy(d => d.DisplayName, StringComparer.OrdinalIgnoreCase)];

    private static (List<Assembly> For, Dictionary<string, Type> Map)? _types;

    /// <summary>
    /// Resolves a type by full name across the core and the loaded modules only (used for cached rom snapshots):
    /// a module that is not loaded contributes nothing, whatever its DLL name.
    /// </summary>
    public static Type? FindType(string fullName)
    {
        var current = _assemblies;
        if (_types is not { } t || !ReferenceEquals(t.For, current))
        {
            var map = new Dictionary<string, Type>(StringComparer.Ordinal);
            foreach (var type in current.SelectMany(SafeTypes))
                if (type.FullName is { } name) map.TryAdd(name, type);
            _types = t = (current, map);
        }
        return t.Map.GetValueOrDefault(fullName);
    }

    public static void Register(IServiceCollection services)
    {
        var types = AllTypes.Where(t => !t.IsAbstract && !t.IsInterface).ToList();

        foreach (var organizer in types.Where(t => typeof(IRomOrganizer).IsAssignableFrom(t)))
            services.AddScoped(typeof(IRomOrganizer), organizer);
        foreach (var resources in types.Where(t => typeof(IDownloadableResources).IsAssignableFrom(t)))
            services.AddSingleton(typeof(IDownloadableResources), resources);
        foreach (var options in types.Where(t => typeof(IConsoleOptions).IsAssignableFrom(t)))
            services.AddSingleton(typeof(IConsoleOptions), options);

        foreach (var console in ConsoleTypes)
        {
            services.AddScoped(typeof(RomParserExecutor<>).MakeGenericType(console));
            services.AddScoped(typeof(TitleInfoProviderManager<>).MakeGenericType(console));

            foreach (var open in new[] { typeof(ITitleInfoProvider<>), typeof(IRomParser<>), typeof(IUpdateVersionProvider<>),
                                         typeof(IRomOrganizer<>), typeof(ILicenseOrganizer<>) })
            {
                var closed = open.MakeGenericType(console);
                foreach (var impl in types.Where(closed.IsAssignableFrom))
                    services.AddScoped(closed, impl);
            }

            // A console-specific manager (e.g. SwitchManager) replaces the generic one and is resolvable by its own type.
            var managerType = typeof(ConsoleManager<>).MakeGenericType(console);
            var specific = types.FirstOrDefault(t => managerType.IsAssignableFrom(t) && t != managerType);
            if (specific is not null)
            {
                services.AddScoped(managerType, specific);
                services.AddScoped(specific);
            }
            else
                services.AddScoped(managerType);
        }

        foreach (var module in Modules)
            module.Register(services);
    }

    private static IEnumerable<Type> SafeTypes(Assembly asm)
    {
        try { return asm.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.OfType<Type>(); }
    }
}

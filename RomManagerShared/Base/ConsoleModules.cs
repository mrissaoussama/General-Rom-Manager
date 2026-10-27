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
/// Finds consoles in <c>RomManagerShared</c> plus any <c>Modules/*.dll</c>, and registers their services.
/// Module assemblies load into the default context so they share <see cref="Rom"/>, EF and DI types.
/// </summary>
public static class ConsoleModules
{
    // The built-in consoles plus the rom model (which lives in RomManager.Abstractions).
    private static readonly Assembly[] CoreAssemblies = [typeof(GamingConsole).Assembly, typeof(Rom).Assembly];
    private static List<Assembly> _assemblies = [.. CoreAssemblies];

    public static IReadOnlyList<Assembly> Assemblies => _assemblies;

    /// <summary>Folder scanned for module DLLs (config <c>GlobalPaths:Modules</c>, default Modules).</summary>
    public static string ModulesDirectory =>
        Configuration.RomManagerConfiguration.GetPathOrDefault("GlobalPaths:Modules", "Modules");

    /// <summary>Loads module DLLs from <paramref name="directory"/>; failures are logged and skipped.</summary>
    public static IReadOnlyList<Assembly> Load(string? directory = null)
    {
        var list = new List<Assembly>(CoreAssemblies);
        directory ??= ModulesDirectory;
        if (Directory.Exists(directory))
        {
            foreach (var dll in Directory.EnumerateFiles(directory, "*.dll"))
            {
                try
                {
                    var asm = Assembly.LoadFrom(dll);
                    if (SafeTypes(asm).Any(t => typeof(GamingConsole).IsAssignableFrom(t)))
                        list.Add(asm);
                }
                catch (Exception ex) { AppLog.Warn($"[Modules] {Path.GetFileName(dll)}: {ex.Message}"); }
            }
        }
        _assemblies = list;
        return list;
    }

    /// <summary>Includes extra assemblies (tests, harness) without touching the file system.</summary>
    public static void Use(params Assembly[] assemblies) =>
        _assemblies = [.. CoreAssemblies.Concat(assemblies).Distinct()];

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

    /// <summary>Resolves a type by full name across loaded modules (used for cached rom snapshots).</summary>
    public static Type? FindType(string fullName) => AllTypes.FirstOrDefault(t => t.FullName == fullName);

    public static void Register(IServiceCollection services)
    {
        var types = AllTypes.Where(t => !t.IsAbstract && !t.IsInterface).ToList();

        foreach (var organizer in types.Where(t => typeof(IRomOrganizer).IsAssignableFrom(t)))
            services.AddScoped(typeof(IRomOrganizer), organizer);
        foreach (var resources in types.Where(t => typeof(IDownloadableResources).IsAssignableFrom(t)))
            services.AddSingleton(typeof(IDownloadableResources), resources);

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

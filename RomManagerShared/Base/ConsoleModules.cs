using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using RomManagerShared.Base.Database;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;

namespace RomManagerShared.Base;

public sealed record ConsoleDescriptor(Type ConsoleType, string Id, string DisplayName);

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
    private static List<Assembly> _assemblies = [typeof(Rom).Assembly];

    public static IReadOnlyList<Assembly> Assemblies => _assemblies;

    /// <summary>Folder scanned for module DLLs (config <c>GlobalPaths:Modules</c>, default Modules).</summary>
    public static string ModulesDirectory =>
        Configuration.RomManagerConfiguration.GetPathOrDefault("GlobalPaths:Modules", "Modules");

    /// <summary>Loads module DLLs from <paramref name="directory"/>; failures are logged and skipped.</summary>
    public static IReadOnlyList<Assembly> Load(string? directory = null)
    {
        var list = new List<Assembly> { typeof(Rom).Assembly };
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
                catch (Exception ex) { FileUtils.Log($"[Modules] {Path.GetFileName(dll)}: {ex.Message}"); }
            }
        }
        _assemblies = list;
        return list;
    }

    /// <summary>Includes extra assemblies (tests, harness) without touching the file system.</summary>
    public static void Use(params Assembly[] assemblies) =>
        _assemblies = [.. new[] { typeof(Rom).Assembly }.Concat(assemblies).Distinct()];

    public static IEnumerable<Type> AllTypes => _assemblies.SelectMany(SafeTypes);

    public static IReadOnlyList<Type> ConsoleTypes =>
        [.. AllTypes.Where(t => typeof(GamingConsole).IsAssignableFrom(t) && !t.IsAbstract && t != typeof(GamingConsole))];

    public static IReadOnlyList<IConsoleModule> Modules =>
        [.. AllTypes.Where(t => typeof(IConsoleModule).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) is not null)
            .Select(t => (IConsoleModule)Activator.CreateInstance(t)!)];

    private static readonly Dictionary<string, string> KnownNames = new()
    {
        ["Switch"] = "Nintendo Switch", ["ThreeDS"] = "Nintendo 3DS", ["DS"] = "Nintendo DS", ["Wii"] = "Nintendo Wii",
        ["WiiU"] = "Nintendo Wii U", ["GameCube"] = "GameCube", ["N64"] = "Nintendo 64", ["NES"] = "NES", ["SNES"] = "SNES",
        ["GameBoy"] = "Game Boy / GBC", ["GameBoyAdvance"] = "Game Boy Advance", ["PS1"] = "PlayStation", ["PS2"] = "PlayStation 2",
        ["PS3"] = "PlayStation 3", ["PS4"] = "PlayStation 4", ["PSP"] = "PSP", ["PSVita"] = "PS Vita", ["Xbox360"] = "Xbox 360",
        ["OriginalXbox"] = "Xbox", ["SegaSaturn"] = "Sega Saturn", ["Genesis"] = "Mega Drive / Genesis",
        ["MasterSystem"] = "Master System", ["GameGear"] = "Game Gear", ["PCEngine"] = "PC Engine / TurboGrafx-16",
    };

    public static ConsoleDescriptor Describe(Type consoleType)
    {
        var module = Modules.FirstOrDefault(m => m.Descriptor.ConsoleType == consoleType);
        if (module is not null) return module.Descriptor;
        var id = consoleType.Name.Replace("Console", "");
        return new ConsoleDescriptor(consoleType, id, KnownNames.GetValueOrDefault(id, id));
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

        foreach (var console in ConsoleTypes)
        {
            services.AddScoped(typeof(GenericRepository<>).MakeGenericType(console));
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

        // Providers may depend on GenericRepository<SomeDto>; register every closed repository a constructor asks for.
        var repoOpen = typeof(GenericRepository<>);
        foreach (var repo in types.SelectMany(t => t.GetConstructors()).SelectMany(c => c.GetParameters())
                     .Select(p => p.ParameterType)
                     .Where(p => p.IsGenericType && p.GetGenericTypeDefinition() == repoOpen)
                     .Distinct())
            services.AddScoped(repo);

        foreach (var module in Modules)
            module.Register(services);
    }

    private static IEnumerable<Type> SafeTypes(Assembly asm)
    {
        try { return asm.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.OfType<Type>(); }
    }
}

// File: testconsole/Program.cs

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RomManagerShared;
using RomManagerShared.Base;
using RomManagerShared.Base.Database;
using RomManagerShared.Configuration;
using RomManagerShared.Interfaces;
using RomManagerShared.Organizers;
using RomManagerShared.Utils;
using System.Reflection;

namespace RomManagerApp
{
    public class Program
    {
        private static Dictionary<Type, string> LoadConsolePaths()
        {
            var consolePaths = new Dictionary<Type, string>();
            var assembly = Assembly.Load("RomManagerShared");

            foreach (var consoleSection in RomManagerConfiguration.Configuration.GetSection("Consoles").GetChildren())
            {
                var consoleName = consoleSection.Key + "Console"; // Match class name convention
                var consoleType = assembly.GetTypes().FirstOrDefault(t =>
                    t.Name.Equals(consoleName, StringComparison.OrdinalIgnoreCase));

                if (consoleType != null)
                {
                    var romPath = consoleSection["RomPath"];
                    if (!string.IsNullOrEmpty(romPath))
                    {
                        consolePaths.Add(consoleType, romPath);
                    }
                }
            }

            return consolePaths;
        }
        // Hardcoded ROM paths per console type.
        private static Dictionary<Type, string> RomPaths = LoadConsolePaths();

        public static async Task Main(string[] args)
        {
            // Load the shared assembly.
            var assembly = Assembly.Load("RomManagerShared");

            var builder = Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Register all console-related services.
                    RegisterConsoleServices(services, assembly);

                    // Register common services.
                    services.AddDbContext<AppDbContext>();
                    services.AddScoped<RomHashRepository>();
                    services.AddScoped<NoIntroRomHashIdentifier>();
                });

            using var host = builder.Build();

            // Load configuration.
            RomManagerConfiguration.Load("config.json");

            // Download required databases.
            await InitializeTitleDatabases();

            // Main loop: select a console and process operations.
            while (true)
            {
                Type selectedConsoleType = SelectConsoleType();
                // Use reflection to invoke the generic processing method:
                var method = typeof(Program)
                    .GetMethod(nameof(ProcessConsoleOperationsGeneric), BindingFlags.NonPublic | BindingFlags.Static);
                var genericMethod = method.MakeGenericMethod(selectedConsoleType);
                await (Task)genericMethod.Invoke(null, new object[] { host.Services });
            }
        }

        private static async Task InitializeTitleDatabases()
        {
            await Task.WhenAll(
                FileDownloader.DownloadSwitchTitleDBFiles(),
                FileDownloader.DownloadSwitchGlobalTitleDBFile(),
                FileDownloader.DownloadSwitchVersionsFile()
            );
        }

        private static Type SelectConsoleType()
        {
            List<Type> consoleTypes = GetAvailableConsoleTypes();
            Console.WriteLine("Available Consoles:");
            for (int i = 0; i < consoleTypes.Count; i++)
            {
                // Remove the word "Console" from the type name.
                Console.WriteLine($"{i + 1}. {consoleTypes[i].Name.Replace("Console", "")}");
            }
            Console.Write("Select a console: ");
            if (int.TryParse(Console.ReadLine(), out int selection) && selection > 0 && selection <= consoleTypes.Count)
            {
                return consoleTypes[selection - 1];
            }
            Console.WriteLine("Invalid selection, try again.");
            return SelectConsoleType();
        }

        private static List<Type> GetAvailableConsoleTypes()
        {
            var assembly = Assembly.Load("RomManagerShared");
            return assembly.GetTypes()
                .Where(t => typeof(GamingConsole).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToList();
        }

        // Generic method that is closed at runtime based on the selected console type.
        private static async Task ProcessConsoleOperationsGeneric<T>(IServiceProvider services) where T : GamingConsole
        {
            // Resolve the concrete ConsoleManager<T> from DI.
            var manager = services.GetRequiredService<ConsoleManager<T>>();

            // Load any console-specific DLLs (using the type name).
            LoadConsoleSpecificDlls(typeof(T).Name);

            Console.WriteLine($"Setup: {manager.GetType()}");
            await manager.Setup();

            // Get the ROM path for T.
            var romPath = RomPaths.TryGetValue(typeof(T), out var path)
                ? path
                : Path.Combine(RomManagerConfiguration.BaseFolder, typeof(T).Name.Replace("Console", ""));
            Console.WriteLine($"Using ROM path: {romPath}");
            Directory.CreateDirectory(romPath);

            await ProcessRomFiles(manager, romPath);
            // Loop for additional operations.
            while (true)
            {
                ShowOperationsMenu();
                string input = Console.ReadLine();
                if (await HandleMenuInput(manager, services, input))
                {
                    break;
                }
            }
        }

        private static async Task ProcessRomFiles<T>(ConsoleManager<T> manager, string romDirectory) where T : GamingConsole
        {
            var supportedExtensions = manager.RomParserExecutor.GetSupportedExtensions();
            var romFiles = FileUtils.GetFilesInDirectoryWithExtensions(romDirectory, supportedExtensions);

            Console.WriteLine($"Found {romFiles.Count} ROM files");
            foreach (var file in romFiles)
            {
                await manager.ProcessFile(file);
            }

            // If the TitleInfoProviderManager exists, enhance ROM title info.
            if (manager.TitleInfoProviderManager != null)
            {
                // (Assuming that GetTitleInfo returns an enhanced Rom.)
                for (int i = 0; i < manager.RomList.Count; i++)
                {
                    manager.RomList[i] = await manager.TitleInfoProviderManager.GetTitleInfo(manager.RomList[i]);
                }
            }
            DisplayProcessedRoms(manager.RomList);
        }

        private static void DisplayProcessedRoms(IEnumerable<Rom> roms)
        {
            Console.WriteLine("Processed ROMs:");
            // Show ROMs grouped by Game, Update, and DLC
            var groupedRoms = roms.GroupBy(rom => rom.GetType().Name);

            foreach (var group in groupedRoms)
            {
                Console.WriteLine($"\n{group.Key}:");
                foreach (var rom in group)
                {
                    Console.WriteLine($"{rom.TitleID} {rom.Titles?.FirstOrDefault()?.Value} {rom.Version}");
                }
            }
        }

        private static void ShowOperationsMenu()
        {
            Console.WriteLine("\nOperations Menu:");
            Console.WriteLine("1. Rescan ROM files");
            Console.WriteLine("2. Rename ROM files");
            Console.WriteLine("3. Check for missing updates");
            Console.WriteLine("4. Organize ROM files");
            Console.WriteLine("5. Return to console selection");
            Console.Write("Select an operation: ");
        }

        private static async Task<bool> HandleMenuInput<T>(ConsoleManager<T> manager, IServiceProvider services, string input) where T : GamingConsole
        {
            switch (input)
            {
                case "1":
                    {
                        string romPath = RomPaths.ContainsKey(typeof(T)) ? RomPaths[typeof(T)] : "C:\\Roms\\Default";
                        await ProcessRomFiles(manager, romPath);
                        break;
                    }
                case "2":
                    {
                        // Implement your file renaming logic here.
                        FileRenamer.RenameFiles(manager.RomList, "{TitleID} {TitleName} [{Region}]");
                        break;
                    }
                case "3":
                    {
                        if (manager is IRomMissingContentChecker checker)
                        {
                            var missingUpdates = await checker.GetMissingUpdates();
                            Console.WriteLine(missingUpdates.Any()
                                ? $"Missing updates: {string.Join("\n", missingUpdates)}"
                                : "All updates available");
                        }
                        break;
                    }
                case "4":
                    {
                        await OrganizeRomFiles(manager, services);
                        break;
                    }
                case "5":
                    return true;
                default:
                    Console.WriteLine("Invalid selection");
                    break;
            }
            return false;
        }

        private static async Task OrganizeRomFiles<T>(ConsoleManager<T> manager, IServiceProvider services) where T : GamingConsole
        {
            // Get available organizers
            var organizers = GetAvailableOrganizers<T>(services);

            if (!organizers.Any())
            {
                Console.WriteLine("No organizers available for this console.");
                return;
            }

            // Display organizers menu
            Console.WriteLine("\nAvailable Organizers:");
            for (int i = 0; i < organizers.Count; i++)
            {
                var organizer = organizers[i];
                string description = organizer is IRomOrganizer nonGenericOrganizer
                    ? nonGenericOrganizer.Description
                    : organizer is IRomOrganizer<T> genericOrganizer
                        ? genericOrganizer.Description
                        : "Unknown Organizer";

                Console.WriteLine($"{i + 1}. {organizer.GetType().Name} - {description}");
            }
            Console.Write("Select an organizer: ");
            if (int.TryParse(Console.ReadLine(), out int selection) && selection > 0 && selection <= organizers.Count)
            {
                var selectedOrganizer = organizers[selection - 1];

                if (selectedOrganizer is IRomOrganizer<T> typedOrganizer)
                {
                    // Use the generic organizer
                    typedOrganizer.Organize(manager.RomList, new List<List<Rom>> { manager.RomList });
                }
                else if (selectedOrganizer is IRomOrganizer nonGenericOrganizer)
                {
                    // Use the non-generic organizer
                    nonGenericOrganizer.Organize(manager.RomList, new List<List<Rom>> { manager.RomList });
                }
                else
                {
                    Console.WriteLine("Selected organizer is not compatible with this console type.");
                    return;
                }

                Console.WriteLine("ROMs organized successfully.");
            }
            else
            {
                Console.WriteLine("Invalid selection.");
            }
        }

        private static List<IBaseRomOrganizer> GetAvailableOrganizers<T>(IServiceProvider services) where T : GamingConsole
        {
            var organizers = new List<IBaseRomOrganizer>();

            // Get console-specific organizers (generic)
            var specificOrganizers = services.GetServices<IRomOrganizer<T>>();
            organizers.AddRange(specificOrganizers);

            // Get non-generic organizers
            var genericOrganizers = services.GetServices<IRomOrganizer>();
            organizers.AddRange(genericOrganizers);

            return organizers;
        }


        private static void LoadConsoleSpecificDlls(string consoleType)
        {
            var consoleName = consoleType.Replace("Console", "");
            var tools = RomManagerConfiguration.Configuration
                .GetSection($"Consoles:{consoleName}:Tools")
                .GetChildren()
                .Select(t => t.Value)
                .ToList();

            foreach (var dll in tools)
            {
                var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dll);
                if (File.Exists(fullPath))
                {
                    Assembly.LoadFrom(fullPath);
                    Console.WriteLine($"Loaded {Path.GetFileName(fullPath)}");
                }
                else
                {
                    Console.WriteLine($"Tool not found: {fullPath}");
                }
            }
        }

        // ************************************************************
        // The DI registration method for all console services.
        // ************************************************************

        private static void RegisterConsoleServices(IServiceCollection services, Assembly assembly)
        {
            var gamingConsoleType = typeof(GamingConsole);

            // Find all gaming console types.
            var consoleTypes = assembly.GetTypes()
                .Where(t => gamingConsoleType.IsAssignableFrom(t)
                            && !t.IsInterface && !t.IsAbstract)
                .ToList();

            // Register non-generic IRomOrganizer implementations.
            var nonGenericOrganizerTypes = assembly.GetTypes()
                .Where(t => typeof(IRomOrganizer).IsAssignableFrom(t)
                            && !t.IsInterface && !t.IsAbstract)
                .ToList();

            foreach (var organizerType in nonGenericOrganizerTypes)
            {
                services.AddScoped(typeof(IRomOrganizer), organizerType);
            }

            foreach (var consoleType in consoleTypes)
            {
                // Register repositories and executors.
                var genericRepositoryType = typeof(GenericRepository<>).MakeGenericType(consoleType);
                var romParserExecutorType = typeof(RomParserExecutor<>).MakeGenericType(consoleType);

                services.AddScoped(genericRepositoryType);
                services.AddScoped(romParserExecutorType);

                // Register all ITitleInfoProvider implementations for the console.
                var titleInfoProviderInterface = typeof(ITitleInfoProvider<>).MakeGenericType(consoleType);
                var titleInfoProviderTypes = assembly.GetTypes()
                    .Where(t => titleInfoProviderInterface.IsAssignableFrom(t)
                                && !t.IsInterface && !t.IsAbstract)
                    .ToList();

                foreach (var titleInfoProviderType in titleInfoProviderTypes)
                {
                    services.AddScoped(titleInfoProviderInterface, titleInfoProviderType);
                }

                // Register all IRomParser implementations for the console.
                var romParserInterface = typeof(IRomParser<>).MakeGenericType(consoleType);
                var romParserTypes = assembly.GetTypes()
                    .Where(t => romParserInterface.IsAssignableFrom(t)
                                && !t.IsInterface && !t.IsAbstract)
                    .ToList();

                foreach (var parserType in romParserTypes)
                {
                    services.AddScoped(romParserInterface, parserType);
                }

                // Register specific services like IUpdateVersionProvider.
                var updateVersionProviderInterface = typeof(IUpdateVersionProvider<>).MakeGenericType(consoleType);
                var updateVersionProviderTypes = assembly.GetTypes()
                    .Where(t => updateVersionProviderInterface.IsAssignableFrom(t)
                                && !t.IsInterface && !t.IsAbstract)
                    .ToList();

                foreach (var versionProviderType in updateVersionProviderTypes)
                {
                    services.AddScoped(updateVersionProviderInterface, versionProviderType);
                }

                // Register ConsoleManager<T>.
                var consoleManagerGenericType = typeof(ConsoleManager<>).MakeGenericType(consoleType);
                services.AddScoped(consoleManagerGenericType);

                // Register console-specific IRomOrganizer<T> implementations.
                var romOrganizerInterface = typeof(IRomOrganizer<>).MakeGenericType(consoleType);
                var romOrganizerTypes = assembly.GetTypes()
                    .Where(t => romOrganizerInterface.IsAssignableFrom(t)
                                && !t.IsInterface && !t.IsAbstract)
                    .ToList();

                foreach (var organizerType in romOrganizerTypes)
                {
                    services.AddScoped(romOrganizerInterface, organizerType);
                }

                // Register ILicenseOrganizer<T> implementations if required.
                var licenseOrganizerInterface = typeof(ILicenseOrganizer<>).MakeGenericType(consoleType);
                var licenseOrganizerTypes = assembly.GetTypes()
                    .Where(t => licenseOrganizerInterface.IsAssignableFrom(t)
                                && !t.IsInterface && !t.IsAbstract)
                    .ToList();

                foreach (var licenseOrganizerType in licenseOrganizerTypes)
                {
                    services.AddScoped(licenseOrganizerInterface, licenseOrganizerType);
                }
            }

        }
    }
}
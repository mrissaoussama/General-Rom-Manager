using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.ThreeDS;
using RomManagerShared.Utils;
using RomManagerShared.Wii.Parsers;
using RomManagerShared.Wii;
using RomManagerShared.DS;
using RomManagerShared.GameBoy;
using RomManagerShared.GameBoyAdvance;
using RomManagerShared.Nintendo64;
using RomManagerShared.OriginalXbox;
using RomManagerShared.PS2;
using RomManagerShared.PS3;
using RomManagerShared.PS4;
using RomManagerShared.PSP;
using RomManagerShared.PSVita;
using RomManagerShared.SegaSaturn;
using RomManagerShared.SNES;
using RomManagerShared.Switch;
using RomManagerShared.WiiU;
using RomManagerShared.Xbox360;
using RomManagerShared;
using RomManagerShared.Configuration;
using RomManagerShared.Base.Database;
using System.Runtime.Intrinsics.Arm;
using AltoMultiThreadDownloadManager;
using RomManagerShared.WiiU.TitleInfoProviders;
using RomManagerShared.WiiU.Parsers;
using System.Reflection;
using RomManagerShared.Switch.TitleInfoProviders;
using RomManagerShared.Switch.Parsers;
using RomManagerShared.Base.Interfaces;
var assembly = Assembly.Load("RomManagerShared");

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddDbContext<AppDbContext>();
        services.AddScoped<RomHashRepository>();
        services.AddScoped(typeof(GenericRepository<>));
        services.AddScoped(typeof(RomParserExecutor<>));
        services.AddScoped(typeof(TitleInfoProviderManager<>));
        RegisterConsoleServices<ThreeDSConsole>(services);
        services.AddScoped(typeof(ConsoleManager<>));
        services.AddScoped<NoIntroRomHashIdentifier>();

        //    RegisterConsoleServices<SwitchConsole>(services, "Switch");
        //    RegisterConsoleServices<WiiUConsole>(services, "WiiU");
        //    RegisterConsoleServices<SNESConsole>(services, "SNES");
    });


static void RegisterConsoleServices<T>(IServiceCollection services) where T : GamingConsole
{
    var assembly = Assembly.Load("RomManagerShared");

    // Register DTOs implementing IExternalRomFormat<T>
    var dtoTypes = assembly.GetTypes()
                           .Where(type => type.IsClass && !type.IsAbstract &&
                                          type.GetInterfaces().Any(inter => inter.IsGenericType &&
                                                                           inter.GetGenericTypeDefinition() == typeof(IExternalRomFormat<>).MakeGenericType(typeof(T))));
    foreach (var dtoType in dtoTypes)
    {
        var serviceType = typeof(GenericRepository<>).MakeGenericType(dtoType);
        services.AddScoped(serviceType);
    }

    // Register TitleInfoProviders
    var titleInfoProviderTypes = assembly.GetTypes()
                                         .Where(type => type.IsClass && !type.IsAbstract &&
                                                        type.IsSubclassOf(typeof(TitleInfoProvider<>).MakeGenericType(typeof(T))));
    foreach (var providerType in titleInfoProviderTypes)
    {
        services.AddScoped(providerType);
        services.AddScoped(typeof(ITitleInfoProvider<>).MakeGenericType(typeof(T)), providerType);
    }

    // Register RomParsers
    var romParserTypes = assembly.GetTypes()
     .Where(type => type.IsClass && !type.IsAbstract &&
                    type.GetInterfaces().Any(inter =>
                        inter.IsGenericType &&
                        inter.GetGenericTypeDefinition() == typeof(IRomParser<>)) &&
                    type.GetInterfaces().Any(inter =>
                        inter.GetGenericArguments().FirstOrDefault() == typeof(T)));
    foreach (var parserType in romParserTypes)
    {
        services.AddScoped(parserType);
    }
    // Register RomParserExecutor<T>
    var executorType = typeof(RomParserExecutor<>).MakeGenericType(typeof(T));
    services.AddScoped(executorType, serviceProvider =>
    {
        var parsers = serviceProvider.GetServices(typeof(IRomParser<>).MakeGenericType(typeof(T)));
        var executorInstance = Activator.CreateInstance(executorType, new[] { parsers });
        return executorInstance;
    });

    // Register IUpdateVersionProvider<T>
    var updateProviderTypes = assembly.GetTypes()
                                      .Where(type => type.IsClass && !type.IsAbstract &&
                                                     type.GetInterfaces().Any(inter => inter.IsGenericType &&
                                                                                      inter.GetGenericTypeDefinition() == typeof(IUpdateVersionProvider<>).MakeGenericType(typeof(T))));
    foreach (var providerType in updateProviderTypes)
    {
        services.AddScoped(providerType);
    }

    // Register ConsoleManager<T>
    //var managerType = typeof(ConsoleManager<>).MakeGenericType(typeof(T));

    //    services.AddScoped(managerType);


    // Register ThreeDSManager if exists
    var specificManagerType = assembly.GetTypes()
        .FirstOrDefault(type => !type.IsAbstract && type.IsSubclassOf(typeof(ConsoleManager<>).MakeGenericType(typeof(T))));
    if (specificManagerType != null)
    {
        services.AddScoped(specificManagerType);
        //   services.AddScoped(typeof(IRomMissingContentChecker), specificManagerType);
    }

}

using var host = builder.Build();
RomManagerConfiguration.Load("config.json");

var noi = host.Services.GetRequiredService<NoIntroRomHashIdentifier>();
var manager = host.Services.GetRequiredService<ConsoleManager<ThreeDSConsole>>();

var rompath = "D:\\nsp\\";
var rompath4 = "D:\\nsp\\errorfiles";
var wiirompath = "C:\\Users\\oussama\\Downloads\\roms\\";
var rompat2h = "C:\\Users\\oussama\\Downloads\\3DS";
var pkgpath = "C:\\Users\\oussama\\Downloads\\roms\\ps3\\1.pkg";


Console.WriteLine($"Setup: {manager.GetType()}");
await manager.Setup();
var implementations = host.Services.GetServices<TitleInfoProvider<ThreeDSConsole>>();

foreach (var implementation in implementations)
{
    if (implementation is ICanSaveToDB)
        await (implementation as ICanSaveToDB).SaveToDatabase();
}
var ext = manager.RomParserExecutor.GetSupportedExtensions();
Console.WriteLine($"Supported Extensions: {string.Join(", ", ext)}");
List<string> filelist = [];
int i = 0; IEnumerable<IEnumerable<string>> splits = null;

await ScanFiles();
Console.WriteLine($"Files found: {filelist.Count}");
List<Task> tasks = [];
//await noi.Setup();
//var roms = await noi.IdentifyRomByHash(filelist.First());
async Task ScanFiles()
{
    filelist = FileUtils.GetFilesInDirectoryWithExtensions(rompat2h, ext);
    manager.RomList.Clear();
    if (manager is IRomMissingContentChecker)
    {
        (manager as IRomMissingContentChecker).GroupedRomList.Clear();
    }
    splits = from item in filelist
             group item by i++ % 5 into part
             select part.AsEnumerable();
}

await ProcessFiles();

async Task ProcessFiles()
{
    foreach (var filegroup in splits)
    {
        foreach (var file in filegroup)
        {
            tasks.Add(manager.ProcessFile(file));
        }
        await Task.WhenAll(tasks);
        tasks.Clear();
    }
}

if (manager.TitleInfoProviderManager != null)
{
    for (int j = 0; j < manager.RomList.Count; j++)
    {
        manager.RomList[j] = await manager.TitleInfoProviderManager.GetTitleInfo(manager.RomList[j]);

    }
}
PrintRoms(manager.RomList);

async void PrintRoms(IEnumerable<Rom> romList)
{
    romList.ToList().ForEach(x => Console.WriteLine($"{x.TitleID} {x.Titles?.FirstOrDefault()?.ToString()} {x.GetType().Name}"));
}
//await HashRoms(manager.RomList);
//async Task HashRoms(IEnumerable<Rom> romList)
//{
//    var repo = host.Services.GetRequiredService<RomHashRepository>();
//    List<Task> tasks = new List<Task>();

//    foreach (var rom in romList)
//    {
//        tasks.Add(HashUtils.CalculateRomHashes(rom, Enum.GetValues<HashTypeEnum>()));
//    }

//    Console.WriteLine("calculating hashes");
//    await Task.WhenAll(tasks);

//    Console.WriteLine("saving new hashes");
//    foreach (var rom in romList)
//    {
//        tasks.Add(repo.AddIfNewRange(rom.Hashes));
//    }
//    await Task.WhenAll(tasks);

//    tasks.Clear();
//}
while (true)
{
    Console.WriteLine("//////////////////////////////////////////");
    Console.WriteLine($"roms found: {manager.RomList.Count}");

    Console.WriteLine("Options:");
    bool canUpdate = false;

    if (manager is IRomMissingContentChecker)
    {
        canUpdate = true;
        Console.WriteLine("Press 0 to check for missing updates");
        Console.WriteLine("Press 1 to organize roms by group");
        Console.WriteLine("Press 2 to organize roms to folders (will move updates and dlcs to game folder");
    }
    Console.WriteLine("Press 3 to rename roms with format {TitleName} [{TitleID}] [{Region}] [v{Version}] [{DLCCount}]");
    Console.WriteLine("Press 4 to Rescan files");
    Console.WriteLine("Press 5 to parse scanned files again");

    {
        string x = Console.ReadLine();
        // string x = "6";
        switch (x)
        {
            case "0":
                if (canUpdate is false) break;
                var missingupdates = await ((IRomMissingContentChecker)manager).GetMissingUpdates();
                missingupdates.ToList().ForEach(x => Console.WriteLine(x.ToString()));
                break;
            case "3":
                FileRenamer.RenameFiles(manager.RomList, "{TitleID} {TitleName} [{Region}] [v{Version}] [{DLCCount}]");
                break;
            case "1":
                ((IRomMissingContentChecker)manager).LoadGroupRomList();
                break;
            case "2":
                foreach (var romGroup in ((IRomMissingContentChecker)manager).GroupedRomList)
                {
                    RomUtils.OrganizeRomsInFolders(romGroup, ((IRomMissingContentChecker)manager).GroupedRomList);

                }
                break;
            case "4":
                Console.WriteLine($"Scanning files...");
                await ScanFiles();
                Console.WriteLine($"Files found: {filelist.Count}");

                break;
            case "5":
                Console.WriteLine($"Scanning roms...");
                await ProcessFiles();
                Console.WriteLine($"roms found: {manager.RomList.Count}");
                PrintRoms(manager.RomList);

                break;
            default: Console.WriteLine("option not valid"); break;
        }
    }

    Console.ReadLine();
}
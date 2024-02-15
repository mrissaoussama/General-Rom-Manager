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
using RomManagerShared.ThreeDS.TitleInfoProviders;
using RomManagerShared.WiiU;
using RomManagerShared.Xbox360;
using RomManagerShared;
using RomManagerShared.Configuration;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddSingleton<ThreeDSJsonTitleInfoProvider>();
        services.AddSingleton<RomParserExecutor>();
        services.AddSingleton<ConsoleManager<ThreeDSConsole>, ThreeDSManager>();
        services.AddSingleton<ConsoleManager<NintendoSwitchConsole>, NintendoSwitchManager>();
        services.AddSingleton<ConsoleManager<NintendoWiiUConsole>, NintendoWiiUManager>();
    });

 using var host = builder.Build();
RomManagerConfiguration.Load("config.json");


var manager = host.Services.GetRequiredService<ConsoleManager<NintendoWiiUConsole>>();

var rompath = "D:\\nsp\\";
var rompath4 = "D:\\nsp\\errorfiles";
var wiirompath = "C:\\Users\\oussama\\Downloads\\roms\\";
var pkgpath = "C:\\Users\\oussama\\Downloads\\roms\\ps3\\1.pkg";

var hashlist = await HashUtils.CalculateFileHashes(Path.Combine(rompath4, "1.nsp"), Enum.GetValues<HashTypeEnum>());
hashlist.ForEach(x => Console.WriteLine(x.ToString()));

Console.WriteLine($"Setup: {manager.GetType()}");
await manager.Setup();
var ext = manager.RomParserExecutor.GetSupportedExtensions();
Console.WriteLine($"Supported Extensions: {string.Join(", ", ext)}");
List<string> filelist = [];
int i = 0; IEnumerable<IEnumerable<string>> splits = null;

await ScanFiles();
Console.WriteLine($"Files found: {filelist.Count}");
List<Task> tasks = [];

async Task ScanFiles()
{
    filelist = FileUtils.GetFilesInDirectoryWithExtensions(wiirompath, ext);
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

PrintRoms(manager.RomList);

void PrintRoms(IEnumerable<Rom> romList)
{
    romList.ToList().ForEach(x => Console.WriteLine($"{x.TitleID} {x.Titles?.FirstOrDefault()?.ToString()} {x.GetType().Name}"));
}

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
    //string x = Console.ReadLine();
    string x = "6";
    switch (x)
    {
        case "0":
            if (canUpdate is false) break;
            var missingupdates = await ((IRomMissingContentChecker)manager).GetMissingUpdates();
            missingupdates.ToList().ForEach(x => Console.WriteLine(x.ToString()));
            break;
        case "3":
            FileRenamer.RenameFiles(manager.RomList, "{TitleName} [{TitleID}] [{Region}] [v{Version}] [{DLCCount}]");
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

//Console.ReadLine();

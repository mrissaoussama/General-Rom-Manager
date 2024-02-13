using DotNet3dsToolkit;
using RomManagerShared;
using System.Text.Json;
using System.IO;
using RomManagerShared.Switch;
using LibHac.FsSystem;
using LibHac.Common.Keys;
using LibHac.Tools.FsSystem.NcaUtils;
using LibHac.Fs.Impl;
using LibHac.FsSrv;
using LibHac.Fs;
using LibHac.Loader;
using LibHac.Common;
using LibHac.Tools.FsSystem;
using LibHac;
using LibHac.Fs.Fsa;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LibHac.Tools.Fs;
using System.Diagnostics;
using System.Collections.Generic;
using RomManagerShared.ThreeDS;
using System.Threading;
using System.Reflection;
using RomManagerShared.Utils;
using RomManagerShared.Wii.Parsers;
using RomManagerShared.Wii;
using RomManagerShared.PS4.Parsers;
using RomManagerShared.PS4;
using RomManagerShared.Base;
using RomManagerShared.GameBoy;
using RomManagerShared.GameBoyAdvance;
using RomManagerShared.DS;
using RomManagerShared.SNES;
using RomManagerShared.PSP;
using RomManagerShared.Interfaces;
using RomManagerShared.Nintendo64;
using RomManagerShared.SegaSaturn;
using RomManagerShared.PSVita;
using RomManagerShared.OriginalXbox;
using RomManagerShared.Xbox360;
using RomManagerShared.Utils.PKGUtils;
using RomManagerShared.PS3;
using RomManagerShared.PS2;
using RomManagerShared.WiiU;
//await WiiUWikiBrewScraper.ScrapeTitles();
Console.WriteLine(WiiUWikiBrewScraper.titles);
RomManagerConfiguration.Load("config.json");
//var result = ((UInt64)(276759 & 0xFFFFFF) << 8) | (0x0004000000000000);
var rompath = "D:\\nsp\\";
var rompath4 = "D:\\nsp\\errorfiles";
var wiirompath = "C:\\Users\\oussama\\Downloads\\roms\\";
var pkgpath = "C:\\Users\\oussama\\Downloads\\roms\\ps3\\1.pkg";
    var hashlist=await HashUtils.CalculateFileHashes(System.IO.Path.Combine(rompath4, "1.nsp"), Enum.GetValues<HashTypeEnum>());
hashlist.ForEach(x => Console.WriteLine(x.ToString()));
var Managers = new List<IConsoleManager>
{
    new NintendoSwitchManager(),
    new ThreeDSManager(),
    new DSManager(),
    new GameBoyManager(),
    new GameBoyAdvanceManager(),
    new PS4Manager(),//5
    new PSPManager(),
    new SNESManager(),
    new WiiManager(),
    new Nintendo64Manager(),
    new SegaSaturnManager(),//10
    new PSVitaManager(),
    new OriginalXboxManager(),
    new Xbox360Manager(),
    new PS3Manager(),
    new PS2Manager(),
    new NintendoWiiUManager(),

};
var manager = Managers[16];
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


while (true)
{
    switch (Console.ReadLine())
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
        default: Console.WriteLine("option not valid");break;
    }
}


Console.ReadLine();
Console.ReadLine();

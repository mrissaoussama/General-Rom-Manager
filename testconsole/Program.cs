using DotNet3dsToolkit;
using RomManagerShared;
using System.Text.Json;
using System.IO;
using testconsole;
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
//HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
RomManagerConfiguration.Load("config.json");
//PluginManager.LoadPlugins(RomManagerConfiguration.GetPluginsPath()); ;


//var result = ((UInt64)(276759 & 0xFFFFFF) << 8) | (0x0004000000000000);
//var id = result.ToString("X16");
//string filename = "L:\\New folder\\3DSCia\\New folder\\0004000e00081e00 - MGS Snake Eater 3D (USA) Update v1.1 ENC.cia";
var rompath = "D:\\nsp\\";
var rompath4 = "D:\\nsp\\1";
var wiirompath = "C:\\Users\\oussama\\Downloads\\Animal Crossing (USA)\\psp";
//var rompath2 = "D:\\cia";
//var ciadirectory = "L:\\3DSCia\\Cias";
//var manager = new ThreeDSManager();
//await manager.Setup();
var manager = new PSPManager();
await manager.Setup();
var ext = manager.RomParserExecutor.GetSupportedExtensions();
var filelist = FileUtils.GetFilesInDirectoryWithExtensions(wiirompath,ext );

int i = 0;
var splits = from item in filelist
             group item by i++ % 5 into part
             select part.AsEnumerable() ;
List<Task> tasks = [];
var cancellationTokenSource = new CancellationTokenSource();

foreach (var filegroup in splits)
{
    foreach (var file in filegroup)
    {
        tasks.Add(manager.ProcessFile(file));

    }
    await Task.WhenAll(tasks);
    tasks.Clear();
}

//manager.RomList.ForEach(x => Console.WriteLine($"{x.TitleID} {x.TitleName} {x.GetType().Name}"));
//FileRenamer.RenameFiles(manager.RomList, "{TitleName} [{TitleID}] [{Region}] [v{Version}] [{DLCCount}]");
//var missingupdates = await manager.ListMissingUpdates();
//foreach (var missingupdate in missingupdates)
//{
//    Console.WriteLine($"Title Id {missingupdate.Item1} Local Update:{missingupdate.Item2} Latest Update: {missingupdate.Item3}");
//    File.AppendAllText("missingupdate.txt",missingupdate.Item1+":"+ missingupdate.Item3+Environment.NewLine);
//}
//foreach (var romGroup in manager.GroupedRomList)
//{
//   RomUtils.OrganizeRomsInFolders(romGroup);

//}
Console.ReadLine();
Console.ReadLine();

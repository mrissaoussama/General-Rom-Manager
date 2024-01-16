using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.PS4;
using RomManagerShared.Switch.Parsers;
using RomManagerShared.Switch.TitleInfoProviders;
using RomManagerShared.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace RomManagerShared.Switch
{
    public class NintendoSwitchManager : IConsoleManager
    {
        public RomParserExecutor RomParserExecutor { get; set; }
        private readonly SwitchJsonTitleInfoProvider titleInfoProvider;
        public SwitchUpdateVersionProvider? UpdateVersionProvider { get; set; }
        public List<Rom> RomList { get; set; }

        public List<List<Rom>> GroupedRomList { get; set; }
        public NintendoSwitchManager()
        {
            RomList = [];
            GroupedRomList = [];
            RomParserExecutor = new RomParserExecutor();
            RomParserExecutor.AddParser(new SwitchRomParser()) ;
            var titlesPath=RomManagerConfiguration.GetSwitchTitleDBPath();
            if (titlesPath == null)
                FileUtils.Log("Switch titles path not found");
            titleInfoProvider = new SwitchJsonTitleInfoProvider(titlesPath);
            var versionjsonPath = RomManagerConfiguration.GetSwitchVersionsPath();
            if(string.IsNullOrEmpty(versionjsonPath))
            {
                FileUtils.Log("Switch version path not found");
                return;
            }    
            UpdateVersionProvider = new(versionjsonPath);

        }
        public async Task Setup()
        {
            List<Task> tasks =
            [
                titleInfoProvider.LoadTitleDatabaseAsync(),
                UpdateVersionProvider.LoadVersionDatabaseAsync()
            ];
            await Task.WhenAll(tasks);
        }
        public async Task ProcessFile(string file)
        {

            var processedlist = await RomParserExecutor.ExecuteParsers(file);

            for (int i = 0; i < processedlist.Count; i++)
            {
                if (processedlist[i].TitleID is null)
                    Console.WriteLine("index {0} is null, filepath={0}", i, file);
                else
                {
                    // Console.WriteLine(processedlist[i].TitleID + " " + processedlist[i].TitleName);
                    var rom = await titleInfoProvider.GetTitleInfo(processedlist[i]);
                    if (rom is not null)
                        processedlist[i] = rom;
                }
            }
            
            RomList.AddRange(processedlist);
        }
        public async Task<List<(string, string, string)>> ListMissingUpdates()
        {
            List<(string, string, string)> missingUpdates = [];
            GroupedRomList = SwitchUtils.GroupRomList(RomList);
            foreach(var romGroup in  GroupedRomList)
            {
                var titleid = romGroup.First().TitleID;
              var latestUpdate=  await UpdateVersionProvider.GetLatestVersion(titleid);
                if (latestUpdate is null || latestUpdate == "0")
                {
                    Console.WriteLine("No updates for {0}", titleid);
                    continue; }
                var updates=romGroup.Where(x=>x is SwitchUpdate).ToList();
                if(updates.Count==0)
                {

                    var tuple = (TitleId: titleid, LocalUpdate: "-1", LatestUpdate: latestUpdate);
                    missingUpdates.Add(tuple);
                    continue;
                }
                var latestLocalUpdate = updates
                    .Select(x => int.TryParse(x.Version, out var versionAsInt) ? versionAsInt : 0)
                    .Max();
                if (latestLocalUpdate > int.Parse(latestUpdate))
                {
                    var tuple = (TitleId: titleid, LocalUpdate: latestLocalUpdate.ToString(), LatestUpdate: latestUpdate);
                    missingUpdates.Add(tuple);
                }
            }
            return missingUpdates;
        }
        public HashSet<string> GetSupportedExtensions()
        {
            if (RomParserExecutor.Parsers.Count == 0)
            {
                return [];
            }
            HashSet<string> extensionhashset = [];
            foreach (var parser in RomParserExecutor.Parsers)
            {
                extensionhashset.UnionWith(parser.Extensions);
            }
            return extensionhashset;
        }
     
    }

}

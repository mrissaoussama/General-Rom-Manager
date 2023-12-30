using RomManagerShared.Switch.Parsers;
using RomManagerShared.Switch.TitleInfoProviders;
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
        private SwitchJsonTitleInfoProvider titleInfoProvider;
        public SwitchUpdateVersionProvider updateVersionProvider { get; set; }
        public List<IRom> RomList { get; set; }
        public List<List<IRom>> GroupedRomList { get; set; }
        public NintendoSwitchManager()
        {
            RomList = new();
            GroupedRomList = new();
            RomParserExecutor = new RomParserExecutor();
            RomParserExecutor.AddParser(new SwitchRomParser())
                ;
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
            updateVersionProvider = new(versionjsonPath);

        }
        public async Task Setup()
        {
            List<Task> tasks = new()
            {
                titleInfoProvider.LoadTitleDatabaseAsync(),
                updateVersionProvider.LoadVersionDatabaseAsync()
            };
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
            List<(string, string, string)> missingUpdates = new();
            GroupedRomList = SwitchUtils.GroupRomList(RomList);
            foreach(var romGroup in  GroupedRomList)
            {
                var titleid = romGroup.First().TitleID;
              var latestUpdate=  await updateVersionProvider.GetLatestVersion(titleid);
                if (latestUpdate is null || latestUpdate == "0")
                {
                    Console.WriteLine("No updates for {0}", titleid);
                    continue; }
                var updates=romGroup.Where(x=>x is SwitchUpdateMetaData).ToList();
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
    }

}

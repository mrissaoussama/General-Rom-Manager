using RomManagerShared;
using RomManagerShared.Switch;
using RomManagerShared.Switch.Parsers;
using RomManagerShared.Switch.TitleInfoProviders;
using RomManagerShared.ThreeDS.TitleInfoProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RomManagerShared.ThreeDS
{
    public class ThreeDSManager : IConsoleManager
    {
        private ThreeDSJsonTitleInfoProvider titleInfoProvider;

        public List<IRom> RomList { get; set; }

        public ThreeDSManager()
        {
            RomList = new();

            RomParserExecutor = new RomParserExecutor();
            RomParserExecutor.AddParser(new ThreeDsRomParser());
            var regionspath=RomManagerConfiguration.GetThreeDSTitleDBPath();
           titleInfoProvider = new ThreeDSJsonTitleInfoProvider(regionspath);
        }

        public RomParserExecutor RomParserExecutor { get ; set; }

        public async Task ProcessFile(string file)
        {     var processedlist = await RomParserExecutor.ExecuteParsers(file);

            for (int i = 0; i < processedlist.Count; i++)
            {
                if (processedlist[i].TitleID is null)
                    Console.WriteLine("index {0} is null, filepath={0}", i, file);
                else
                {
                    processedlist[i] =await titleInfoProvider.GetTitleInfo(processedlist[i]); 
                }
            }

            RomList.AddRange(processedlist);
        }

        public async Task Setup()
        {
            await titleInfoProvider.LoadTitleDatabaseAsync();
        }
    }
}

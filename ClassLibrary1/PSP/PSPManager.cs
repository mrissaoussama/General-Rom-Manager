using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.PSP.Parsers;
using RomManagerShared.Switch.Parsers;
using RomManagerShared.Switch.TitleInfoProviders;
using RomManagerShared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RomManagerShared.PSP
{
    public class PSPManager : IConsoleManager
    {
        public RomParserExecutor RomParserExecutor { get ; set; }
        public List<Rom> RomList { get ; set ; }

        public PSPManager()
        {
            RomList = [];
            RomParserExecutor = new RomParserExecutor();
        }
        public async Task ProcessFile(string file)
        {
            var processedlist = await RomParserExecutor.ExecuteParsers(file);

            RomList.AddRange(processedlist);
        }

        public Task Setup()
        {
            RomParserExecutor.AddParser(new PSPRomParser());
            return Task.CompletedTask;
        }

    }
}

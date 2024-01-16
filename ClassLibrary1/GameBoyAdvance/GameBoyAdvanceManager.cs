using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.GameBoyAdvance.Parsers;
using RomManagerShared.Switch.Parsers;
using RomManagerShared.Switch.TitleInfoProviders;
using RomManagerShared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RomManagerShared.GameBoyAdvance
{
    public class GameBoyAdvanceManager : IConsoleManager
    {
        public RomParserExecutor RomParserExecutor { get ; set; }
        public List<Rom> RomList { get ; set ; }

        public GameBoyAdvanceManager()
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
            RomParserExecutor.AddParser(new GameBoyAdvanceRomParser());
            return Task.CompletedTask;
        }

    }
}

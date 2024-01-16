using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.PS4.Parsers;
using RomManagerShared.Switch.Parsers;
using RomManagerShared.Switch.TitleInfoProviders;
using RomManagerShared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RomManagerShared.PS4
{
    public class PS4Manager : IConsoleManager
    {
        public RomParserExecutor RomParserExecutor { get ; set; }
        public List<Rom> RomList { get ; set ; }
        public List<List<Rom>> GroupedRomList { get; set; }
        PS4PKGUpdateAndDLCChecker Checker { get; set; }

        public PS4Manager()
        {
            RomList = [];
            GroupedRomList = [];
            RomParserExecutor = new RomParserExecutor();
            Checker = new();
        }
        public async Task ProcessFile(string file)
        {
            var processedlist = await RomParserExecutor.ExecuteParsers(file);

            RomList.AddRange(processedlist);
        }

        public Task Setup()
        {
            RomParserExecutor.AddParser(new PS4PKGParser());
            return Task.CompletedTask;
        }
        public async Task<List<Rom>> GetMissingDLC(Rom ps4game)
        {
            List<Rom> dlclist = [];
            List<Rom> missingdlc = [];
            // list.Add( await checker.CheckForUpdate(rom));

            if (ps4game is not PS4Game)
                return missingdlc;
            dlclist.AddRange(await Checker.GetDLCList(ps4game));
            if(dlclist.Count==0)
            {
                Console.WriteLine($"Game {ps4game.TitleName} | {ps4game.TitleID} has no dlc");
                return dlclist;
            }
            missingdlc = await Checker.GetMissingDLC(ps4game, RomList, dlclist);
            if (missingdlc.Count > 0)
            {
                Console.WriteLine($"Game {ps4game.TitleName} is missing {missingdlc.Count}");
                foreach (var dlc in missingdlc)
                {
                    Console.WriteLine($"{dlc.TitleName}: {dlc.ProductCode}");
                }

            }else
            {
                Console.WriteLine($"Game {ps4game.TitleName} | {ps4game.TitleID} has all dlcs locally");
                return missingdlc;
            }
            return missingdlc;
        }
        public async Task<Rom?> GetMissingUpdate(Rom ps4game)
        {
            if (ps4game is not PS4Game)
                return null;

            Rom? latestUpdate = GetLatestLocalUpdate(ps4game);

            if (latestUpdate == null)
            {
                Console.WriteLine("No local updates found.");
            }

            var onlineUpdate = await Checker.CheckForUpdate(ps4game);

            if (onlineUpdate != null)
            {
                Console.WriteLine($"Online Update Version: {onlineUpdate.Version}");

                if (latestUpdate is not null && Version.Parse(onlineUpdate.Version) > Version.Parse(((PS4Update)latestUpdate).Version))
                {
                    Console.WriteLine($"{ps4game.TitleName} is out of date. local:{latestUpdate.Version}| latest:{onlineUpdate.Version}");
                }
                else if (latestUpdate is not null && Version.Parse(onlineUpdate.Version) == Version.Parse(((PS4Update)latestUpdate).Version))
                {
                    Console.WriteLine($"{ps4game.TitleName} is updated {latestUpdate.Version}");
                }
                else
                {
                    Console.WriteLine($"{ps4game.TitleName} has no local updates, latest update: {onlineUpdate.Version}");
                }
            }
            else
            {
                Console.WriteLine("No online updates found.");
                return null;
            }

            return onlineUpdate;
        }


        public Rom? GetLatestLocalUpdate(Rom ps4game)
        {
            List<Rom> relatedUpdates = RomList
                .Where(rom => rom.TitleID == ps4game.TitleID && rom is PS4Update)
                .ToList();

            if (relatedUpdates.Count == 0)
            {
                return null;
            }
            // Find the update with the highest version
            PS4Update latestUpdate = (PS4Update)relatedUpdates
                .OrderByDescending(rom => Version.Parse(((PS4Update)rom).Version))
                .First();
            Console.WriteLine("Updates found:");
            foreach (var update in relatedUpdates)
            {
                Console.WriteLine($"Version: {((PS4Update)update).Version}");
            }

            return latestUpdate;
        }
    }
}

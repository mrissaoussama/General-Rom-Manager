using RomManagerShared.Base;
using RomManagerShared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Param_SFO;
using System.Security.Cryptography;
namespace RomManagerShared.GameBoy.Parsers
{
    public class GameBoyRomParser : IRomParser
    {
        public GameBoyRomParser()
        {
            Extensions = ["gb","gbc"];
        }
        public HashSet<string> Extensions { get; set; }
        
            public Task<List<Rom>> ProcessFile(string path)
        {
            GameBoyGame gameboyrom = new();
            var metadatareader = new GameBoyMetadataReader();
            var metadata=metadatareader.GetMetadata(path);
            gameboyrom.Version = metadata.MaskRomVersionNumber;
            gameboyrom.TitleName = metadata.Title;
            gameboyrom.TitleID = metadata.GameCode;
            //gameboyrom.Hash = metadata.HeaderChecksum;
            gameboyrom.Hash = metadata.StoredGlobalChecksum;
            gameboyrom.Size = FileUtils.GetFileSize(path);
            if (metadata.DestinationCode=="00")
            {
                gameboyrom.Region = "J";
            }
            else
            {
                gameboyrom.Region = "Unknown";
            }
            Console.WriteLine(gameboyrom.ToString());
            List<Rom> list = [gameboyrom];
     
            return Task.FromResult(list);
        }
    }
}

using RomManagerShared.Base;
using RomManagerShared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Param_SFO;
using System.Security.Cryptography;
namespace RomManagerShared.GameBoyAdvance.Parsers
{
    public class GameBoyAdvanceRomParser : IRomParser
    {
        public GameBoyAdvanceRomParser()
        {
            Extensions = ["gba","agb"] ;
        }
        public HashSet<string> Extensions { get; set; }
        
            public Task<List<Rom>> ProcessFile(string path)
        {
            GameBoyAdvanceGame GameBoyAdvancerom = new();
            var metadatareader = new GameBoyAdvanceMetadataReader();
            var metadata=metadatareader.GetMetadata(path);
            GameBoyAdvancerom.Version = metadata.VersionCode;
            GameBoyAdvancerom.TitleName = metadata.Title;
            GameBoyAdvancerom.TitleID = metadata.GameCode;
            char lastCharacter = metadata.GameCode[3];
            switch (lastCharacter)
            {
                case 'J':
                    { 
                    GameBoyAdvancerom.Region="Japan";
                    GameBoyAdvancerom.Languages.Add("JP"); break;
                    }
                case 'P':
                    GameBoyAdvancerom.Region = "Europe";
                     break; 
                case 'F':
                    GameBoyAdvancerom.Region = "French";
                    GameBoyAdvancerom.Languages.Add("FR"); break;
                case 'S':
                    GameBoyAdvancerom.Region = "Spain";
                    GameBoyAdvancerom.Languages.Add("ES"); break;
                case 'E':
                    GameBoyAdvancerom.Region = "USA";
                    GameBoyAdvancerom.Languages.Add("EN"); break;
                case 'D':
                   GameBoyAdvancerom.Region = "Germany";
                    GameBoyAdvancerom.Languages.Add("DE"); break;
                case 'I':
                    GameBoyAdvancerom.Region = "Italy";
                    GameBoyAdvancerom.Languages.Add("IT"); break;
              
            }        
            GameBoyAdvancerom.Size = FileUtils.GetFileSize(path);
   
            Console.WriteLine(GameBoyAdvancerom.ToString());
            List<Rom> list = [GameBoyAdvancerom];
     
            return Task.FromResult(list);
        }
    }
}

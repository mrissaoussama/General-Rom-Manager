using RomManagerShared.Base;
using RomManagerShared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Param_SFO;
using System.Security.Cryptography;
namespace RomManagerShared.SNES.Parsers
{
    public class SNESRomParser : IRomParser
    {
        public SNESRomParser()
        {
            Extensions = ["sfc"];
        }
        public HashSet<string> Extensions { get; set; }

        public Task<List<Rom>> ProcessFile(string path)
        {
            SNESGame SNESrom = new();
            var metadatareader = new SNESMetadataReader();
            var metadata = metadatareader.GetMetadata(path);
            SNESrom.TitleName = metadata.Name;
            SNESrom.Version = metadata.VersionNumber.ToString() ;
            if (metadata.CountryCode ==0x1)
            {
                SNESrom.Region = "USA";
            }
            else
            {
                SNESrom.Region = "EU";

            }
            SNESrom.Size = FileUtils.GetFileSize(path);
         
            Console.WriteLine(SNESrom.ToString());
            List<Rom> list = [SNESrom];
     
            return Task.FromResult(list);
        }

    }
}

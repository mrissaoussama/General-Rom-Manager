using RomManagerShared.Base;
using RomManagerShared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Param_SFO;
using System.Security.Cryptography;
namespace RomManagerShared.DS.Parsers
{
    public class DSRomParser : IRomParser
    {
        public DSRomParser()
        {
            Extensions = ["nds"];
        }
        public HashSet<string> Extensions { get; set; }
        
            public Task<List<Rom>> ProcessFile(string path)
        {
            DSGame DSrom = new();
            var metadatareader = new DSMetadataReader();
            var metadata=metadatareader.GetMetadata(path);
            DSrom.TitleName = metadata.Title;
            DSrom.TitleID = metadata.GameCode;
            DSrom.Version = metadata.RomVersion;
            
            DSrom.Size = FileUtils.GetFileSize(path);
         
            Console.WriteLine(DSrom.ToString());
            List<Rom> list = [DSrom];
     
            return Task.FromResult(list);
        }

    }
}

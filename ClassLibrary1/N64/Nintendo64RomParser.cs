using RomManagerShared.Base;
using RomManagerShared.Nintendo64;
using RomManagerShared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RomManagerShared.Nintendo64.Parsers
{
    public class Nintendo64RomParser : IRomParser
    {
        public Nintendo64RomParser()
        {
            Extensions = ["n64", "v64","z64"];
        }
        public HashSet<string> Extensions { get; set; }
        public Task<HashSet<Rom>> ProcessFile(string path)
        {
            Nintendo64Game n64rom = Z64Utils.Z64Utils.ParseRom(path);
            HashSet<Rom> list = [n64rom];
            return Task.FromResult(list);
        }
    }
}

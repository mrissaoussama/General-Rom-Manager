using RomManagerShared.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RomManagerShared.Wii.Parsers
{
    public class WiiWadRomParser : IRomParser
    {
        public HashSet<string> Extensions { get; set; }
        public WiiWadRomParser()
    {
        Extensions = ["wad"];
    }

        public Task<List<Rom>> ProcessFile(string path)
        {
            WadInfo wadInfo = new();
            byte[] wadfile = WadInfo.LoadFileToByteArray(path);
            WiiWadGame wiiWadGame = new();
            wiiWadGame.TitleID= wadInfo.GetTitleID(wadfile, 0);
            wiiWadGame.Region = wadInfo.GetRegionFlag(wadfile);
            wiiWadGame.Type = wadInfo.GetChannelType(wadfile, 0);
            wiiWadGame.Version = wadInfo.GetTitleVersion(wadfile).ToString();
            //wiiWadGame.Languages=[..wadInfo.GetChannelTitles(wadfile)];
            wiiWadGame.TitleName = wadInfo.GetChannelTitles(wadfile)[1];
            List<Rom> list = [wiiWadGame];
            return Task.FromResult(list);
        }
    }
}

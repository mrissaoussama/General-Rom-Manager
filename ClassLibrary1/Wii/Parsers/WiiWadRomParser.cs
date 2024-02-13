using RomManagerShared.Base;
namespace RomManagerShared.Wii.Parsers;

public class WiiWadRomParser : IRomParser
{
    public HashSet<string> Extensions { get; set; }
    public WiiWadRomParser()
    {
        Extensions = ["wad"];
    }    public Task<HashSet<Rom>> ProcessFile(string path)
    {
        WadInfo wadInfo = new();
        byte[] wadfile = WadInfo.LoadFileToByteArray(path);
        WiiWadGame wiiWadGame = new()
        {
            TitleID = wadInfo.GetTitleID(wadfile, 0),
            //  wiiWadGame.Region = wadInfo.GetRegionFlag(wadfile);
            ChannelType = wadInfo.GetChannelType(wadfile, 0),
            Version = wadInfo.GetTitleVersion(wadfile).ToString()
        };
        //wiiWadGame.Languages=[..wadInfo.GetChannelTitles(wadfile)];
        wiiWadGame.AddTitleName(wadInfo.GetChannelTitles(wadfile)[1]);
        HashSet<Rom> list = [wiiWadGame];
        return Task.FromResult(list);
    }
}

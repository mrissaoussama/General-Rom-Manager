using RomManagerShared.Base;
using RomManagerShared.Interfaces;

namespace RomManagerShared.Nintendo64.Parsers;

public class Nintendo64RomParser : IRomParser<N64Console>
{
    public Nintendo64RomParser()
    {
        Extensions = ["n64", "v64", "z64"];
    }
    public List<string> Extensions { get; set; }
    public Task<List<Rom>> ProcessFile(string path)
    {
        Nintendo64Game n64rom = Z64Utils.Z64Utils.ParseRom(path);
        List<Rom> list = [n64rom];
        return Task.FromResult(list);
    }
}

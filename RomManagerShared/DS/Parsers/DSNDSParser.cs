using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
namespace RomManagerShared.DS.Parsers;

public class DSNDSParser : IRomParser<DSConsole>
{
    public DSNDSParser()
    {
        Extensions = ["nds"];
    }
    public List<string> Extensions { get; set; }
    public Task<List<Rom>> ProcessFile(string path)
    {
        DSGame DSrom = new();
        var metadatareader = new DSMetadataReader();
        var metadata = DSMetadataReader.GetMetadata(path);
        DSrom.AddTitleName(metadata.Title);
        DSrom.TitleID = metadata.GameCode;
        DSrom.Version = metadata.RomVersion;
        DSrom.Size = FileUtils.GetFileSize(path);
        DSrom.Path = path;

        Console.WriteLine(DSrom.ToString());
        List<Rom> list = [DSrom];
        return Task.FromResult(list);
    }
}

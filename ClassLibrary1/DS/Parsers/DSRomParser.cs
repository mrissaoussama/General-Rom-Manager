using RomManagerShared.Base;
using RomManagerShared.Utils;
namespace RomManagerShared.DS.Parsers
{
    public class DSRomParser : IRomParser
    {
        public DSRomParser()
        {
            Extensions = ["nds"];
        }
        public HashSet<string> Extensions { get; set; }
        public Task<HashSet<Rom>> ProcessFile(string path)
        {
            DSGame DSrom = new();
            var metadatareader = new DSMetadataReader();
            var metadata = metadatareader.GetMetadata(path);
            DSrom.AddTitleName(metadata.Title);
            DSrom.TitleID = metadata.GameCode;
            DSrom.Version = metadata.RomVersion;
            DSrom.Size = FileUtils.GetFileSize(path);
            Console.WriteLine(DSrom.ToString());
            HashSet<Rom> list = [DSrom];
            return Task.FromResult(list);
        }
    }
}

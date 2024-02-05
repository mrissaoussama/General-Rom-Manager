using RomManagerShared.Base;
using RomManagerShared.Utils;
namespace RomManagerShared.SNES.Parsers
{
    public class SNESRomParser : IRomParser
    {
        public SNESRomParser()
        {
            Extensions = ["sfc"];
        }
        public HashSet<string> Extensions { get; set; }
        public static IEnumerable<string> GetRegionAndLanguage(Rom DSrom)
        {
            char lastCharacter = DSrom.TitleID[2];            switch (lastCharacter)
            {
                case 'U':
                    yield return "U";
                    yield return "English";
                    break;
            }
        }
        public Task<HashSet<Rom>> ProcessFile(string path)
        {
            SNESGame SNESrom = new();
            var metadatareader = new SNESMetadataReader();
            var metadata = metadatareader.GetMetadata(path);
            SNESrom.AddTitleName(metadata.Name);
            SNESrom.Version = metadata.VersionNumber.ToString();
            if (metadata.CountryCode == 0x1)
            {
                SNESrom.AddRegion(Region.USA);
            }
            else
            {
                SNESrom.AddRegion(Region.Europe);            }
            SNESrom.Size = FileUtils.GetFileSize(path);
            Console.WriteLine(SNESrom.ToString());
            HashSet<Rom> list = [SNESrom];
            return Task.FromResult(list);
        }    }
}

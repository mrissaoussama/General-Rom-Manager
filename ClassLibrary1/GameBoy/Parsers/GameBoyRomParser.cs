using RomManagerShared.Base;
using RomManagerShared.Utils;
namespace RomManagerShared.GameBoy.Parsers
{
    public class GameBoyRomParser : IRomParser
    {
        public GameBoyRomParser()
        {
            Extensions = ["gb", "gbc"];
        }
        public HashSet<string> Extensions { get; set; }
        public Task<HashSet<Rom>> ProcessFile(string path)
        {
            GameBoyGame gameboyrom = new();
            var metadatareader = new GameBoyMetadataReader();
            var metadata = metadatareader.GetMetadata(path);
            gameboyrom.Version = metadata.MaskRomVersionNumber;
            gameboyrom.AddTitleName(metadata.Title);
            gameboyrom.TitleID = metadata.GameCode;
            //gameboyrom.Hash = metadata.HeaderChecksum;
            gameboyrom.Size = FileUtils.GetFileSize(path);
            if (metadata.DestinationCode == "00")
            {
                gameboyrom.AddRegion(Region.Japan);
            }
            else
            {
                gameboyrom.AddRegion(Region.Unknown);
            }
            Console.WriteLine(gameboyrom.ToString());
            HashSet<Rom> list = [gameboyrom];
            return Task.FromResult(list);
        }
    }
}

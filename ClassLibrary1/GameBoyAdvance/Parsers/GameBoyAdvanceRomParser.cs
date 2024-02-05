using RomManagerShared.Base;
using RomManagerShared.DS;
using RomManagerShared.Utils;
namespace RomManagerShared.GameBoyAdvance.Parsers
{
    public class GameBoyAdvanceRomParser : IRomParser
    {
        public GameBoyAdvanceRomParser()
        {
            Extensions = ["gba", "agb"];
        }
        public HashSet<string> Extensions { get; set; }
        public Task<HashSet<Rom>> ProcessFile(string path)
        {
            GameBoyAdvanceGame GameBoyAdvancerom = new();
            var metadatareader = new GameBoyAdvanceMetadataReader();
            var metadata = metadatareader.GetMetadata(path);
            GameBoyAdvancerom.Version = metadata.VersionCode;
            GameBoyAdvancerom.AddTitleName(metadata.Title);
            GameBoyAdvancerom.TitleID = metadata.GameCode;
            char lastCharacter = metadata.GameCode[3];
            switch (lastCharacter)
            {
                case 'J':
                    GameBoyAdvancerom.AddRegion(Region.Japan);
                    GameBoyAdvancerom.AddLanguage(Language.Japanese);
                    break;
                case 'P':
                    GameBoyAdvancerom.AddRegion(Region.Europe);
                    break;
                case 'F':
                    GameBoyAdvancerom.AddRegion(Region.France);
                    GameBoyAdvancerom.AddLanguage(Language.French);
                    break;
                case 'S':
                    GameBoyAdvancerom.AddRegion(Region.Spain);
                    GameBoyAdvancerom.AddLanguage(Language.Spanish);
                    break;
                case 'E':
                    GameBoyAdvancerom.AddRegion(Region.USA);
                    GameBoyAdvancerom.AddLanguage(Language.English);
                    break;
                case 'D':
                    GameBoyAdvancerom.AddRegion(Region.Germany);
                    GameBoyAdvancerom.AddLanguage(Language.German);
                    break;
                case 'I':
                    GameBoyAdvancerom.AddRegion(Region.Italy);
                    GameBoyAdvancerom.AddLanguage(Language.Italian);
                    break;
            }            GameBoyAdvancerom.Size = FileUtils.GetFileSize(path);
            GameBoyAdvancerom.Path = path;
            Console.WriteLine(GameBoyAdvancerom.ToString());
            HashSet<Rom> list = [GameBoyAdvancerom];            return Task.FromResult(list);
        }
    }
}

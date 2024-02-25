using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
namespace RomManagerShared.GameBoy.Parsers;

public class GameBoyRomParser : IRomParser<GameBoyConsole>
{
    public GameBoyRomParser()
    {
        Extensions = ["gb", "gbc"];
    }
    public List<string> Extensions { get; set; }
    public Task<List<Rom>> ProcessFile(string path)
    {
        GameBoyGame gameboyrom = new();
        var metadatareader = new GameBoyMetadataReader();
        var metadata = GameBoyMetadataReader.GetMetadata(path);
        gameboyrom.Version = metadata.MaskRomVersionNumber;
        gameboyrom.AddTitleName(metadata.Title);
        gameboyrom.TitleID = metadata.GameCode;
        //gameboyrom.Hash = metadata.HeaderChecksum;
        gameboyrom.Size = FileUtils.GetFileSize(path);
        gameboyrom.Path = path;
        if (metadata.DestinationCode == "00")
        {
            gameboyrom.AddRegion(Region.Japan);
        }
        else
        {
            gameboyrom.AddRegion(Region.Unknown);
        }
        Console.WriteLine(gameboyrom.ToString());
        List<Rom> list = [gameboyrom];
        return Task.FromResult(list);
    }
}

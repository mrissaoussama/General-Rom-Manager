using DotNet3dsToolkit;
using RomManagerShared.Base;
using RomManagerShared.Utils;
using static RomManagerShared.ThreeDS.ThreeDSUtils;
namespace RomManagerShared.ThreeDS;

public class ThreeDsRomParser : IRomParser
{
    public ThreeDsRomParser()
    {
        Extensions = ["cia"];
    }
    public HashSet<string> Extensions { get; set; }
    public async Task<HashSet<Rom>> ProcessFile(string path)
    {
        ThreeDsRom rom = new();
        Console.WriteLine(Path.GetFileName(path));
        try
        {
            await rom.OpenFile(path);
        }
        catch
        {
            FileUtils.Log($"error reading 3DS file {path}. make sure the file is valid and not encrypted ");
        }
        var titleid = rom.GetTitleID().ToString("X16");
        Rom game = GetRomType(titleid);
        game.ProductCode = rom.GetProductCode();
        game.AddDescription(rom.GetShortDescription());
        game.AddTitleName(rom.GetLongDescription().Replace("\n", " "));
        game.Publisher = rom.GetPublisher();
        game.AddRegion(rom.GetRegion());
        game.Path = path;
        game.TitleID = titleid;
        game.Version = rom.GetTitleVersion().ToString();
        game.MinimumFirmware = rom.GetSystemVersion().ToString();
        return [game];
    }
    private static Rom GetRomType(string titleId)
    {
        var romType = DetectContentCategory(titleId);
        switch (romType)
        {
            case TidCategory.Normal:
                ThreeDSGame game = new();
                return game;
            case TidCategory.Update:
                ThreeDSUpdate update = new();
                return update;
            case TidCategory.Dlc:
            case TidCategory.AddOnContents:
                ThreeDSDLC dlc = new();
                return dlc;
            default:
                return new ThreeDSGame();
        }
    }
}

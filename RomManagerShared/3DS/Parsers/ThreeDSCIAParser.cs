using DotNet3dsToolkit;
using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
namespace RomManagerShared.ThreeDS;

public class ThreeDSCIAParser : IRomParser<ThreeDSConsole>
{
    public ThreeDSCIAParser()
    {
        Extensions = ["cia"];
    }
    public List<string> Extensions { get; set; }
    public async Task<List<Rom>> ProcessFile(string path)
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
        Rom game = ThreeDSUtils.GetRomType(titleid);
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
}

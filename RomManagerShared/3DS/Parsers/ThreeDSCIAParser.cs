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
        using ThreeDsRom rom = new();
        try
        {
            await rom.OpenFile(path);
        }
        catch (Exception ex)
        {
            FileUtils.Log($"error reading 3DS file {path}: {ex.Message}. make sure the file is valid and not encrypted");
            return [];
        }
        var titleid = rom.GetTitleID().ToString("X16");
        Rom game = ThreeDSUtils.GetRomType(titleid);
        game.ProductCode = rom.GetProductCode();
        // Updates/DLC have no SMDH; titles come from title DB providers.
        if (rom.GetShortDescription() is { } shortDesc)
            game.AddDescription(shortDesc);
        if (rom.GetLongDescription() is { } longDesc)
            game.AddTitleName(longDesc.Replace("\n", " "));
        game.Publisher = rom.GetPublisher();
        Region r = rom.GetRegion();
        game.AddRegion(r);
        game.Path = path;
        game.TitleID = titleid;
        game.Version = rom.GetTitleVersion().ToString();
        game.MinimumFirmware = rom.GetSystemVersion().ToString();
        game.Icon = IconCache.Save("3DS", titleid, rom.GetIconPng());
        return [game];
    }
}

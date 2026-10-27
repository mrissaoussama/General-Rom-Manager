using DotNet3dsToolkit;
using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
namespace RomManagerShared.ThreeDS.Parsers;

/// <summary>
/// Cartridge images (.3ds/.cci) and single partitions (.cxi). The header gives the title ID and product
/// code even for encrypted dumps; decrypted dumps also yield the SMDH title, publisher, region and icon.
/// </summary>
public class ThreeDSCartridgeParser : IRomParser<ThreeDSConsole>
{
    public List<string> Extensions { get; set; } = ["3ds", "cci", "cxi"];

    public async Task<List<Rom>> ProcessFile(string path)
    {
        ThreeDSCartridgeHeader? header;
        using (var stream = FileUtils.OpenRead(path))
            header = ThreeDSCartridgeHeader.TryRead(stream);
        if (header is null)
        {
            AppLog.Info($"[3DS] {Path.GetFileName(path)}: no NCSD/NCCH header");
            return [];
        }

        var game = ThreeDSUtils.GetRomType(header.TitleId);
        game.TitleID = header.TitleId;
        game.ProductCode = header.ProductCode;
        game.Path = path;
        game.Size = FileUtils.GetFileSize(path);
        if (header.Encrypted)
            return [game]; // ExeFS is encrypted; the title comes from the title DB providers

        try
        {
            using ThreeDsRom rom = new();
            await rom.OpenFile(path);
            ThreeDSCIAParser.ApplySmdh(game, rom, header.TitleId);
        }
        catch (Exception ex)
        {
            AppLog.Warn($"[3DS] {Path.GetFileName(path)}: SMDH not readable: {ex.Message}");
        }
        return [game];
    }
}

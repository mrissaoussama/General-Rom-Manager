using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;

namespace RomManagerShared.WiiU.Parsers;

/// <summary>Disc images: only the product code is readable without the per-disc key; names come from the title DB.</summary>
public class WiiUWudWuxParser : IRomParser<WiiUConsole>
{
    public List<string> Extensions { get; set; }

    public WiiUWudWuxParser()
    {
        Extensions = ["wud", "wux"];
    }

    public Task<List<Rom>> ProcessFile(string path)
    {
        try
        {
            var productCode = WiiUFormats.ReadDiscProductCode(path);
            if (productCode is null)
                return Task.FromResult<List<Rom>>([]);

            var game = new WiiUGame { Path = path, ProductCode = productCode, Size = FileUtils.GetFileSize(path) };
            game.AddRegion(WiiUFormats.RegionFromProductCode(productCode));
            return Task.FromResult<List<Rom>>([game]);
        }
        catch (Exception ex)
        {
            FileUtils.Log($"[WiiU disc] {path}: {ex.Message}");
            return Task.FromResult<List<Rom>>([]);
        }
    }
}

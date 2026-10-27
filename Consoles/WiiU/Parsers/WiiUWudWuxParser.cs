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

            // With the disc key the partition table also gives exact title IDs.
            var key = WiiUFormats.FindDiscKey(path);
            List<string?> titleIds = key is null ? [] : [.. WiiUFormats.ReadDiscTitleIds(path, key)];
            if (titleIds.Count == 0)
                titleIds.Add(null);

            List<Rom> roms = [];
            foreach (var titleId in titleIds)
            {
                var type = titleId is null ? typeof(WiiUGame) : WiiUUtils.GetRomMetadataClass(titleId) ?? typeof(WiiUGame);
                var rom = (Rom)Activator.CreateInstance(type)!;
                rom.TitleID = titleId;
                rom.Path = path;
                rom.ProductCode = productCode;
                rom.Size = FileUtils.GetFileSize(path);
                rom.AddRegion(WiiUFormats.RegionFromProductCode(productCode));
                // Placeholder until the title DB fills in the real name.
                rom.AddTitleName(System.Text.RegularExpressions.Regex.Replace(Path.GetFileNameWithoutExtension(path), @"\s*\[[^\]]*\]", "").Trim());
                roms.Add(rom);
            }
            return Task.FromResult(roms);
        }
        catch (Exception ex)
        {
            AppLog.Warn($"[WiiU disc] {path}: {ex.Message}");
            return Task.FromResult<List<Rom>>([]);
        }
    }
}

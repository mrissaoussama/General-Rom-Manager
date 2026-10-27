using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
namespace RomManagerShared.PS4.Parsers;

public class PS4PKGParser : IRomParser<PS4Console>
{
    public List<string> Extensions { get; set; } = ["pkg"];

    public Task<List<Rom>> ProcessFile(string path)
    {
        PS4Pkg pkg;
        using (var stream = FileUtils.OpenRead(path))
        {
            if (!PS4PkgReader.IsPS4Pkg(stream))
                return Task.FromResult<List<Rom>>([]); // PS3/PSP/Vita PKGs share the extension
            pkg = PS4PkgReader.Read(stream);
        }
        if (pkg.Sfo is not { } sfo)
        {
            FileUtils.Log($"[PS4] {Path.GetFileName(path)}: no readable param.sfo");
            return Task.FromResult<List<Rom>>([]);
        }

        // CATEGORY: gd = game, gp = patch, ac = add-on content.
        Rom rom = sfo.Category.ToLowerInvariant() switch
        {
            "gp" => new PS4Update(),
            "ac" => new PS4DLC(),
            _ => new PS4Game(),
        };
        rom.ProductCode = pkg.ContentId;
        rom.TitleID = sfo.TitleId;
        rom.Version = sfo.AppVersion;
        rom.MinimumFirmware = sfo.MinimumFirmware;
        if (sfo.Title is { } title) rom.AddTitleName(title);

        // Keyed by content ID (+ patch suffix) since game, patch and DLC share the title ID.
        var key = (pkg.ContentId is { Length: > 0 } id ? id : rom.TitleID) + (rom is PS4Update ? "_patch" : "");
        rom.Icon = IconCache.Save("PS4", key, pkg.Icon0);
        rom.Thumbnail = IconCache.Save("PS4", key + "_pic0", pkg.Pic0);
        if (IconCache.Save("PS4", key + "_pic1", pkg.Pic1) is { } pic1)
            rom.AddImage(pic1);
        rom.Path = path;
        rom.Size = FileUtils.GetFileSize(path);
        return Task.FromResult<List<Rom>>([rom]);
    }
}

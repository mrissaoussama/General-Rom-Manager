using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
using RomManagerShared.Utils.PKGUtils;

namespace RomManagerShared.PS3.Parsers;

public class PS3PKGRomParser : IRomParser<PS3Console>
{
    public PS3PKGRomParser()
    {
        Extensions = ["pkg"];
    }
    public List<string> Extensions { get; set; }

    public Task<List<Rom>> ProcessFile(string path)
    {
        try
        {
            if (!PsnPkgReader.IsPkg3(path))
                return Task.FromResult<List<Rom>>([]);
            var info = PsnPkgReader.Read(path);
            if (info.Platform != PsnPlatform.PS3)
                return Task.FromResult<List<Rom>>([]);

            Rom rom = info.Kind switch
            {
                PsnPkgKind.DLC => new PS3DLC(),
                PsnPkgKind.Update => new PS3Update(),
                _ => new PS3Game(),
            };
            return Task.FromResult<List<Rom>>([PsnPkgRomMapper.Fill(rom, info, path)]);
        }
        catch (Exception ex)
        {
            FileUtils.Log($"[PS3 PKG] {path}: {ex.Message}");
            return Task.FromResult<List<Rom>>([]);
        }
    }
}

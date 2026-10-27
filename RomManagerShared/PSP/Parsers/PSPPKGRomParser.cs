using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
using RomManagerShared.Utils.PKGUtils;

namespace RomManagerShared.PSP.Parsers;

public class PSPPKGRomParser : IRomParser<PSPConsole>, IContentSniffer
{
    public bool CanParse(string path, ReadOnlySpan<byte> header) => FileMagic.IsPkg3(header);

    public PSPPKGRomParser()
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
            // PS1 classics are PSP packages too.
            if (info.Platform is not (PsnPlatform.PSP or PsnPlatform.PSX))
                return Task.FromResult<List<Rom>>([]);

            Rom rom = info.Kind switch
            {
                PsnPkgKind.DLC => new PSPDLC(),
                PsnPkgKind.Update => new PSPUpdate(),
                _ => new PSPGame(),
            };
            return Task.FromResult<List<Rom>>([PsnPkgRomMapper.Fill(rom, info, path)]);
        }
        catch (Exception ex)
        {
            AppLog.Warn($"[PSP PKG] {path}: {ex.Message}");
            return Task.FromResult<List<Rom>>([]);
        }
    }
}

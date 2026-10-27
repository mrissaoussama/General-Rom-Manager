using DiscUtils.Iso9660;
using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
namespace RomManagerShared.PSP.Parsers;

public class PSPRomParser : IRomParser<PSPConsole>, IContentSniffer
{
    // Only an ISO 9660 image that names another system is ruled out.
    public bool CanParse(string path, ReadOnlySpan<byte> header) =>
        !FileMagic.HasExtension(path, "iso") || FileMagic.IsoSystemId(header) is null or "PSP GAME";

    private const string SfoPath = @"PSP_GAME\PARAM.SFO";
    private const string IconPath = @"PSP_GAME\ICON0.PNG";

    public PSPRomParser()
    {
        Extensions = ["iso", "cso", "zso", "pbp"];
    }
    public List<string> Extensions { get; set; }

    public Task<List<Rom>> ProcessFile(string path)
    {
        try
        {
            if (path.EndsWith(".pbp", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(ParsePbp(path));

            using var isoStream = FileUtils.OpenDiscImage(path);
            using var cd = new CDReader(isoStream, true);
            if (!cd.FileExists(SfoPath))
                return Task.FromResult<List<Rom>>([]);

            ParamSfo sfo;
            using (var sfoStream = cd.OpenFile(SfoPath, FileMode.Open, FileAccess.Read))
                sfo = ParamSfo.Read(sfoStream);
            byte[]? icon = null;
            if (cd.FileExists(IconPath))
            {
                using var iconStream = cd.OpenFile(IconPath, FileMode.Open, FileAccess.Read);
                icon = new byte[iconStream.Length];
                iconStream.ReadExactly(icon);
            }
            return Task.FromResult<List<Rom>>([FromSfo(sfo, path, icon)]);
        }
        catch (Exception ex)
        {
            AppLog.Warn($"[PSP] {path}: {ex.Message}");
            return Task.FromResult<List<Rom>>([]);
        }
    }

    // EBOOT.PBP (PSN games, PS1 classics) or PARAM.PBP.
    private static List<Rom> ParsePbp(string path)
    {
        using var fs = FileUtils.OpenRead(path);
        var pbp = PbpReader.Read(fs);
        return pbp?.Sfo is { } sfo ? [FromSfo(sfo, path, pbp.Icon0)] : [];
    }

    private static PSPGame FromSfo(ParamSfo sfo, string path, byte[]? icon)
    {
        var pspRom = new PSPGame
        {
            TitleID = sfo.GetString("DISC_ID") ?? sfo.GetString("TITLE_ID"),
            Category = sfo.GetString("CATEGORY") ?? string.Empty,
            Version = sfo.GetString("DISC_VERSION") ?? sfo.GetString("APP_VER"),
            MinimumFirmware = sfo.GetString("PSP_SYSTEM_VER"),
            DiskNumber = sfo.GetInt("DISC_NUMBER") ?? 1,
            DiskTotal = sfo.GetInt("DISC_TOTAL") ?? 1,
            ParentalLevel = sfo.GetInt("PARENTAL_LEVEL") ?? 0,
            Path = path,
        };
        pspRom.AddTitleName(sfo.GetString("TITLE") ?? string.Empty);
        SetRegion(pspRom);
        pspRom.Icon = IconCache.Save("PSP", pspRom.TitleID, icon);
        return pspRom;
    }

    private static void SetRegion(PSPGame pspRom)
    {
        if (pspRom.TitleID is not { Length: > 2 } id)
        {
            pspRom.AddRegion(Region.Unknown);
            return;
        }
        pspRom.AddRegion(id[2] switch
        {
            'U' => Region.USA,
            'E' => Region.Europe,
            'J' => Region.Japan,
            'A' => Region.Asia,
            'H' => Region.HongKong,
            'K' => Region.Korea,
            _ => Region.Unknown,
        });
    }
}

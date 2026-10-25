using DiscUtils.Iso9660;
using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
namespace RomManagerShared.PSP.Parsers;

public class PSPRomParser : IRomParser<PSPConsole>
{
    private const string SfoPath = @"PSP_GAME\PARAM.SFO";

    public PSPRomParser()
    {
        Extensions = ["iso", "cso"];
    }
    public List<string> Extensions { get; set; }

    public Task<List<Rom>> ProcessFile(string path)
    {
        try
        {
            using var isoStream = FileUtils.OpenDiscImage(path);
            using var cd = new CDReader(isoStream, true);
            if (!cd.FileExists(SfoPath))
                return Task.FromResult<List<Rom>>([]);

            using var sfoStream = cd.OpenFile(SfoPath, FileMode.Open, FileAccess.Read);
            var sfo = ParamSfo.Read(sfoStream);

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
            return Task.FromResult<List<Rom>>([pspRom]);
        }
        catch (Exception ex)
        {
            FileUtils.Log($"[PSP] {path}: {ex.Message}");
            return Task.FromResult<List<Rom>>([]);
        }
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

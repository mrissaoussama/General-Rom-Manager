using RomManagerShared.Base;
using RomManagerShared.PSVITA;
using RomManagerShared.Utils;
using RomManagerShared.Utils.PKGUtils;
using System.IO.Compression;
using System.Xml;

namespace RomManagerShared.PSP.Parsers;

public class PSPPKGRomParser : IRomParser
{
    public PSPPKGRomParser()
    {
        Extensions = ["pkg"];
    }
    public HashSet<string> Extensions { get; set; }
    //string[] gameCategories = { "AC", "GC", "GDC" };
    public Task<HashSet<Rom>> ProcessFile(string path)
    {
        HashSet<Rom> list = [];
        if (Path.GetExtension(path).ToLower().Contains("pkg"))
        {
            try
            {//todo find a good way to keep the parsed dto if it belong to another system to avoid parsing again
                var dto = PSNPackageInfoFetcher.FetchPackageInfo(path);
                if (dto.PkgPlatform != "PSP")
                    throw new Exception("Not a PSP pkg");
                Rom PSProm;
                if (dto.PkgType == "GAME")
                    PSProm = new PSPGame();
                else if (dto.PkgType == "DLC")
                    PSProm = new PSPDLC();
                else PSProm = new PSPUpdate();
                PSProm.TitleID = dto.TitleId;
                PSProm.AddTitleName(dto.Title);
                PSProm.AddRegion(GetRegion(dto.Region));
                PSProm.MinimumFirmware = dto.MinFirmware.ToString() ;
                PSProm.Version = dto.Version.ToString();
                PSProm.Path = path;
                PSProm.ProductCode = dto.ContentId;
                list.Add(PSProm);
            }
            catch (Exception ex)
            {
                FileUtils.Log(ex.Message);

            }
        }
        return Task.FromResult(list);
    }

    private static Region GetRegion(string region)
    {
        return region switch
        {
            "US" => Region.USA,
            "JP" => Region.Japan,
            "UK" => Region.UnitedKingdom,
            "EU" => Region.Europe,
            "KR" => Region.Korea,
            "SA" => Region.Asia,
            "TW" => Region.Taiwan,
            "RU" => Region.Russia,
            "CN" => Region.China,
            _ => Region.Unknown,
        };
    }
}

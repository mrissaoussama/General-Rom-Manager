using RomManagerShared.Base;
using RomManagerShared.PSVITA;
using RomManagerShared.Utils;
using RomManagerShared.Utils.PKGUtils;
using System.IO.Compression;
using System.Xml;

namespace RomManagerShared.PS3.Parsers;

public class PS3PKGRomParser : IRomParser
{
    public PS3PKGRomParser()
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
                if (dto.PkgPlatform != "PS3")
                    throw new Exception("Not a ps3 pkg");
                Rom ps3rom;
                if (dto.PkgType == "GAME")
                    ps3rom = new PS3Game();
                else if (dto.PkgType == "DLC")
                    ps3rom = new PS3DLC();
                else ps3rom = new PS3Update();
                ps3rom.TitleID = dto.TitleId;
                ps3rom.AddTitleName(dto.Title);
                ps3rom.AddRegion(GetRegion(dto.Region));
                ps3rom.MinimumFirmware = dto.MinFirmware.ToString() ;
                ps3rom.Version = dto.Version.ToString();
                ps3rom.Path = path;
                ps3rom.ProductCode = dto.ContentId;
                list.Add(ps3rom);
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

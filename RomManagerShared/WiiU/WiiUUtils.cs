using RomManagerShared.Base;using RomManagerShared.Utils;using System.Xml.Linq;namespace RomManagerShared.WiiU;

public class WiiUUtils
{
    public static Type? GetRomMetadataClass(string titleId)
    {
        if (titleId.StartsWith("00050000") && titleId.Length == 16)
        {
            return typeof(WiiUGame);
        }
        else if (titleId.StartsWith("0005000E") && titleId.Length == 16)
        {
            return typeof(WiiUUpdate);
        }
        else if (titleId.StartsWith("0005000C") && titleId.Length == 16)
        {
            return typeof(WiiUDLC);
        }
        else
        {
            FileUtils.Log($"Unknown title ID format: {titleId}");
            return null;
        }
    }
    public static Rom ParseAppXml(string xmlContent, Rom rom)
    {
        XDocument doc = XDocument.Parse(xmlContent);

        string? titleId = doc.Descendants("title_id").FirstOrDefault()?.Value;
        string? titleVersion = doc.Descendants("title_version").FirstOrDefault()?.Value;

        if (!string.IsNullOrEmpty(titleId))
            rom.TitleID = titleId;
        if (!string.IsNullOrEmpty(titleVersion))
            rom.Version = titleVersion;
        return rom;
    }

    // meta.xml suffix -> language for longname_xx / publisher_xx.
    private static readonly (string Suffix, Language Language)[] MetaLanguages =
    [
        ("ja", Language.Japanese), ("en", Language.English), ("fr", Language.French), ("de", Language.German),
        ("it", Language.Italian), ("es", Language.Spanish), ("zhs", Language.SimplifiedChinese), ("ko", Language.Korean),
        ("nl", Language.Dutch), ("pt", Language.Portuguese), ("ru", Language.Russian), ("zht", Language.TraditionalChinese),
    ];

    public static Rom ParseMetaXml(string xmlContent, Rom rom)
    {
        XDocument doc = XDocument.Parse(xmlContent);
        string? Value(string name) => doc.Descendants(name).FirstOrDefault()?.Value is { Length: > 0 } v ? v : null;

        // Long names are the full titles; short names are usually the same text and are not added again.
        foreach (var (suffix, language) in MetaLanguages)
            if (Value("longname_" + suffix) is { } name)
                rom.AddTitleName(name.Replace("\n", " "), language);
        rom.Publisher = Value("publisher_en") ?? MetaLanguages.Select(l => Value("publisher_" + l.Suffix)).FirstOrDefault(p => p is not null);
        rom.Version = Value("title_version");
        rom.ProductCode = Value("product_code");
        return rom;
    }
    //    public static List<List<Rom>> GroupRomList(IEnumerable<Rom> romList)
    //    {
    //        Dictionary<string, List<Rom>> romGroups = [];
    //        foreach (var rom in romList)
    //        {
    //            if (rom.TitleID is null)
    //                continue;
    //            string modifiedTitleId = GetIdentifyingTitleID(rom.TitleID);
    //            if (!romGroups.ContainsKey(modifiedTitleId))
    //            {
    //                romGroups[modifiedTitleId] = [];
    //            }
    //            romGroups[modifiedTitleId].Add(rom);
    //        }
    //        List<List<Rom>> groupedRomList = new(
    //  romGroups.Values.Select(group => new List<Rom>(
    //      group.OrderBy(rom => rom is Game)
    //  ))
    //);
    //        return groupedRomList;
    //    }
    //public static string GetIdentifyingTitleID(string titleId)
    //{
    //    return titleId[..^3];
    //}

}

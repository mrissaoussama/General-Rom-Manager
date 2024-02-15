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
            Console.WriteLine($"Unknown title ID format: {titleId}");
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

    public static Rom ParseMetaXml(string xmlContent, Rom rom)
    {
        XDocument doc = XDocument.Parse(xmlContent);

        string productCode = doc.Descendants("product_code").FirstOrDefault()?.Value;
        string companyCode = doc.Descendants("company_code").FirstOrDefault()?.Value;
        string titleVersion = doc.Descendants("title_version").FirstOrDefault()?.Value;
        string region = doc.Descendants("region").FirstOrDefault()?.Value;
        string pcCero = doc.Descendants("pc_cero").FirstOrDefault()?.Value;
        string pcEsrb = doc.Descendants("pc_esrb").FirstOrDefault()?.Value;
        string pcBbfc = doc.Descendants("pc_bbfc").FirstOrDefault()?.Value;
        string pcUsk = doc.Descendants("pc_usk").FirstOrDefault()?.Value;
        string pcPegiGen = doc.Descendants("pc_pegi_gen").FirstOrDefault()?.Value;
        string pcPegiFin = doc.Descendants("pc_pegi_fin").FirstOrDefault()?.Value;
        string pcPegiPrt = doc.Descendants("pc_pegi_prt").FirstOrDefault()?.Value;
        string pcPegiBbfc = doc.Descendants("pc_pegi_bbfc").FirstOrDefault()?.Value;
        string pcCob = doc.Descendants("pc_cob").FirstOrDefault()?.Value;
        string pcGrb = doc.Descendants("pc_grb").FirstOrDefault()?.Value;
        string pcCgsrr = doc.Descendants("pc_cgsrr").FirstOrDefault()?.Value;
        string pcOflc = doc.Descendants("pc_oflc").FirstOrDefault()?.Value;
        string Nunchuk = doc.Descendants("ext_dev_nunchaku").FirstOrDefault()?.Value;
        string Classiccontroller = doc.Descendants("ext_dev_classic").FirstOrDefault()?.Value;
        string Devhardwareport = doc.Descendants("ext_dev_urcc").FirstOrDefault()?.Value;
        string USBkeyboard = doc.Descendants("ext_dev_board").FirstOrDefault()?.Value;
        string extDevUsbKeyboard = doc.Descendants("ext_dev_usb_keyboard").FirstOrDefault()?.Value;
        string extDevEtc = doc.Descendants("ext_dev_etc").FirstOrDefault()?.Value;
        string extDevEtcName = doc.Descendants("ext_dev_etc_name").FirstOrDefault()?.Value;
        string gamepadsupport = doc.Descendants("drc_use").FirstOrDefault()?.Value;
        string networkUse = doc.Descendants("network_use").FirstOrDefault()?.Value;
        string onlineAccountUse = doc.Descendants("online_account_use").FirstOrDefault()?.Value;
        string lnJapanese = doc.Descendants("longname_ja").FirstOrDefault()?.Value;
        string lnEnglish = doc.Descendants("longname_en").FirstOrDefault()?.Value;
        string lnFrench = doc.Descendants("longname_fr").FirstOrDefault()?.Value;
        string lnGerman = doc.Descendants("longname_de").FirstOrDefault()?.Value;
        string lnItalian = doc.Descendants("longname_it").FirstOrDefault()?.Value;
        string lnSpanish = doc.Descendants("longname_es").FirstOrDefault()?.Value;
        string lnSimplifiedChinese = doc.Descendants("longname_zhs").FirstOrDefault()?.Value;
        string lnKorean = doc.Descendants("longname_ko").FirstOrDefault()?.Value;
        string lnDutch = doc.Descendants("longname_nl").FirstOrDefault()?.Value;
        string lnPortuguese = doc.Descendants("longname_pt").FirstOrDefault()?.Value;
        string lnRussian = doc.Descendants("longname_ru").FirstOrDefault()?.Value;
        string lnTraditionalChinese = doc.Descendants("longname_zht").FirstOrDefault()?.Value;

        string snJapanese = doc.Descendants("shortname_ja").FirstOrDefault()?.Value;
        string snEnglish = doc.Descendants("shortname_en").FirstOrDefault()?.Value;
        string snFrench = doc.Descendants("shortname_fr").FirstOrDefault()?.Value;
        string snGerman = doc.Descendants("shortname_de").FirstOrDefault()?.Value;
        string snItalian = doc.Descendants("shortname_it").FirstOrDefault()?.Value;
        string snSpanish = doc.Descendants("shortname_es").FirstOrDefault()?.Value;
        string snSimplifiedChinese = doc.Descendants("shortname_zhs").FirstOrDefault()?.Value;
        string snKorean = doc.Descendants("shortname_ko").FirstOrDefault()?.Value;
        string snDutch = doc.Descendants("shortname_nl").FirstOrDefault()?.Value;
        string snPortuguese = doc.Descendants("shortname_pt").FirstOrDefault()?.Value;
        string snRussian = doc.Descendants("shortname_ru").FirstOrDefault()?.Value;
        string snTraditionalChinese = doc.Descendants("shortname_zht").FirstOrDefault()?.Value;
        string pbJapanese = doc.Descendants("publisher_ja").FirstOrDefault()?.Value;
        string pbEnglish = doc.Descendants("publisher_en").FirstOrDefault()?.Value;
        string pbFrench = doc.Descendants("publisher_fr").FirstOrDefault()?.Value;
        string pbGerman = doc.Descendants("publisher_de").FirstOrDefault()?.Value;
        string pbItalian = doc.Descendants("publisher_it").FirstOrDefault()?.Value;
        string pbSpanish = doc.Descendants("publisher_es").FirstOrDefault()?.Value;
        string pbSimplifiedChinese = doc.Descendants("publisher_zhs").FirstOrDefault()?.Value;
        string pbKorean = doc.Descendants("publisher_ko").FirstOrDefault()?.Value;
        string pbDutch = doc.Descendants("publisher_nl").FirstOrDefault()?.Value;
        string pbPortuguese = doc.Descendants("publisher_pt").FirstOrDefault()?.Value;
        string pbRussian = doc.Descendants("publisher_ru").FirstOrDefault()?.Value;
        string pbTraditionalChinese = doc.Descendants("publisher_zht").FirstOrDefault()?.Value;
        Dictionary<Language, string> longlanguageMap = new Dictionary<Language, string>
{

    { Language.Japanese, lnJapanese },
    { Language.English, lnEnglish },
    { Language.French, lnFrench },
    { Language.Italian, lnItalian },
    { Language.German, lnGerman },
    { Language.Spanish, lnSpanish },
    { Language.SimplifiedChinese, lnSimplifiedChinese },
    { Language.Korean, lnKorean },
    { Language.Dutch, lnDutch },
    { Language.Portuguese, lnPortuguese },
    { Language.Russian, lnRussian },
    { Language.TraditionalChinese, lnTraditionalChinese }
}
.Where(kv => !string.IsNullOrEmpty(kv.Value))
.ToDictionary(kv => kv.Key, kv => kv.Value);
        foreach (var kvp in longlanguageMap)
        {
            string longName = kvp.Value;
            Language language = kvp.Key;

            if (!string.IsNullOrEmpty(longName))
            {
                rom.AddTitleName(longName, language);
            }
        }
        Dictionary<Language, string> shortlanguageMap = new Dictionary<Language, string>
{
    { Language.Japanese, snJapanese },
    { Language.English, snEnglish },
    { Language.French, snFrench },
    { Language.Italian, snItalian },
    { Language.German, snGerman },
    { Language.Spanish, snSpanish },
    { Language.SimplifiedChinese, snSimplifiedChinese },
    { Language.Korean, snKorean },
    { Language.Dutch, snDutch },
    { Language.Portuguese, snPortuguese },
    { Language.Russian, snRussian },
    { Language.TraditionalChinese, snTraditionalChinese }
}
.Where(kv => !string.IsNullOrEmpty(kv.Value))
.ToDictionary(kv => kv.Key, kv => kv.Value);
        foreach (var kvp in longlanguageMap)
        {
            string longName = kvp.Value;
            Language language = kvp.Key;

            if (!string.IsNullOrEmpty(longName))
            {
                rom.AddTitleName(longName, language);
            }
        }
        rom.Publisher = pbEnglish;
        rom.Version = titleVersion;
        rom.ProductCode = productCode;
        return rom;
    }
    //    public static HashSet<HashSet<Rom>> GroupRomList(IEnumerable<Rom> romList)
    //    {
    //        Dictionary<string, HashSet<Rom>> romGroups = [];
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
    //        HashSet<HashSet<Rom>> groupedRomList = new(
    //  romGroups.Values.Select(group => new HashSet<Rom>(
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

using RomManagerShared.Base;

namespace RomManagerShared.ThreeDS;

public class ThreeDSUtils
{
    /// <summary>
    /// GameTDB box art for a product code such as <c>CTR-P-AMKE</c> (the last 4 characters are the game code; its
    /// last letter is the region); null when the code is not one.
    /// </summary>
    public static string? GameTdbCoverUrl(string? productCode)
    {
        if (string.IsNullOrWhiteSpace(productCode)) return null;
        var code = productCode.Trim().ToUpperInvariant();
        if (code.Length < 4) return null;
        code = code[^4..];
        if (!code.All(char.IsAsciiLetterOrDigit)) return null;
        var region = code[3] switch
        {
            'E' => "US",
            'J' => "JA",
            'K' => "KO",
            'D' => "DE",
            'F' => "FR",
            'S' => "ES",
            'I' => "IT",
            'H' => "NL",
            'R' => "RU",
            'C' or 'W' => "ZH",
            _ => "EN",
        };
        return $"https://art.gametdb.com/3ds/cover/{region}/{code}.jpg";
    }

    public enum TidCategory
    {
        Normal = 0x0,
        DlpChild = 0x1,
        Demo = 0x2,
        AddOnContents = 0x4,
        NoExecute = 0x8,
        Update = 0xE,
        System = 0x10,
        NotMount = 0x80,
        Dlc = 0x8C,
        Twl = 0x8000,
        DSIware = 0x8004
    }
    public static TidCategory DetectContentCategory(string titleId)
    {
        int contentCategory = GetContentCategory(titleId);
        TidCategory category = (TidCategory)contentCategory;
        return category;
    }
    static int GetContentCategory(string titleId)
    {
        int abcd = int.Parse(titleId.Substring(4, 4), System.Globalization.NumberStyles.HexNumber);
        return abcd;
    }
    public static string GetIdentifyingTitleID(string titleId)
    {
        return titleId[8..];
    }
    public static List<List<Rom>> GroupRomList(IEnumerable<Rom> romList)
    {
        Dictionary<string, List<Rom>> romGroups = [];
        foreach (var rom in romList)
        {
            if (string.IsNullOrEmpty(rom.TitleID))
                continue;
            string modifiedTitleId = GetIdentifyingTitleID(rom.TitleID);
            if (!romGroups.ContainsKey(modifiedTitleId))
            {
                romGroups[modifiedTitleId] = [];
            }
            romGroups[modifiedTitleId].Add(rom);
        }
        List<List<Rom>> groupedRomList = new(
  romGroups.Values.Select(group => new List<Rom>(
      group.OrderBy(rom => rom is Game)
  ))
);
        return groupedRomList;
    }
    public static Rom GetRomType(string titleId)
    {
        var romType = DetectContentCategory(titleId);
        switch (romType)
        {
            case TidCategory.Normal:
                ThreeDSGame game = new();
                return game;
            case TidCategory.Update:
                ThreeDSUpdate update = new();
                return update;
            case TidCategory.Dlc:
            case TidCategory.AddOnContents:
                ThreeDSDLC dlc = new();
                return dlc;
            case TidCategory.Twl:
            case TidCategory.DSIware:
                ThreeDSDSIWare dsi = new();
                return dsi;
        
            default:
                return new ThreeDSGame();
        }
    }
}

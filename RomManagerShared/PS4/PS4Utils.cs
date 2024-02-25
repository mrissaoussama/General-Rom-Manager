using RomManagerShared.Base;
using RomManagerShared.Utils;
namespace RomManagerShared.PS4;

public class PS4Utils
{
    static byte[] PKG_Magic = [127, 67, 78, 84];    public static bool IsPS4PKG(string path)
    {
        FileStream pkgstream = new(path, FileMode.Open, FileAccess.Read);        using BinaryReader binaryReader = new(pkgstream);
        byte[] a = binaryReader.ReadBytes(4);
        return BinUtils.CompareBytes(a, PKG_Magic);
    }    public static string SystemFirmwareLongToString(long value)
    {
        string hexOutput = String.Format("{0:X}", value);
        if (value != 0)
        {
            string first_three = hexOutput[..3];
            return first_three.Insert(1, ".");
        }
        else
            return "0";
    }    public static string GetTitleIDFromProductCode(string? productCode)
    {
        int startIndex = productCode.IndexOf('-') + 1;
        int endIndex = productCode.IndexOf('_', startIndex);
        var titleid = productCode[startIndex..endIndex];
        return titleid;
    }

    public static List<List<Rom>> GroupRomList(IEnumerable<Rom> romList)
    {
        Dictionary<string, List<Rom>> romGroups = [];        foreach (var rom in romList)
        {
            if (rom.TitleID is null)
                continue;            if (!romGroups.ContainsKey(rom.TitleID))
            {
                romGroups[rom.TitleID] = [];
            }            romGroups[rom.TitleID].Add(rom);
        }
        List<List<Rom>> groupedRomList = new(
  romGroups.Values.Select(group => new List<Rom>(
      group.OrderBy(rom => rom is Game)
  ))
);
        return groupedRomList;
    }
}

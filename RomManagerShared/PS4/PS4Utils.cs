﻿using RomManagerShared.Base;
using RomManagerShared.Utils;
namespace RomManagerShared.PS4;

public class PS4Utils
{
    static byte[] PKG_Magic = [127, 67, 78, 84];
    {
        FileStream pkgstream = new(path, FileMode.Open, FileAccess.Read);
        byte[] a = binaryReader.ReadBytes(4);
        return BinUtils.CompareBytes(a, PKG_Magic);
    }
    {
        string hexOutput = String.Format("{0:X}", value);
        if (value != 0)
        {
            string first_three = hexOutput[..3];
            return first_three.Insert(1, ".");
        }
        else
            return "0";
    }
    {
        int startIndex = productCode.IndexOf('-') + 1;
        int endIndex = productCode.IndexOf('_', startIndex);
        var titleid = productCode[startIndex..endIndex];
        return titleid;
    }

    public static List<List<Rom>> GroupRomList(IEnumerable<Rom> romList)
    {
        Dictionary<string, List<Rom>> romGroups = [];
        {
            if (rom.TitleID is null)
                continue;
            {
                romGroups[rom.TitleID] = [];
            }
        }
        List<List<Rom>> groupedRomList = new(
  romGroups.Values.Select(group => new List<Rom>(
      group.OrderBy(rom => rom is Game)
  ))
);
        return groupedRomList;
    }
}
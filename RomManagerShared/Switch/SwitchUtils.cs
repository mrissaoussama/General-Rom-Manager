﻿using RomManagerShared.Base;

public class SwitchUtils
{
    public static Type? GetRomMetadataClass(string titleId)
    {
        if (titleId.EndsWith("000") && titleId.Length == 16)
        {
            return typeof(SwitchGame);
        }
        else if (titleId.EndsWith("800") && titleId.Length == 16)
        {
            return typeof(SwitchUpdate);
        }
        else if (titleId.Length == 16 && IsHexBetween001AndFFF(titleId.Substring(13, 3)) && !titleId.Substring(13, 3).Equals("800", StringComparison.OrdinalIgnoreCase))
        {
            return typeof(SwitchDLC);
        }
        else
        {
            Console.WriteLine($"Unknown title ID format: {titleId}");
            return null;
        }
    }
    public static List<List<Rom>> GroupRomList(IEnumerable<Rom> romList)
    {
        Dictionary<string, List<Rom>> romGroups = [];
        {
            if (string.IsNullOrEmpty(rom.TitleID))
                continue;
            {
                romGroups[modifiedTitleId] = [];
            }
        }
        List<List<Rom>> groupedRomList = new(
  romGroups.Values.Select(group => new List<Rom>(
      group.OrderBy(rom => rom is Game)
  ))
);
        return groupedRomList;
    }
    {
        return titleId[..^3];
    }
    {
        int decimalValue;
        return int.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber, null, out decimalValue)
            && decimalValue >= 1
            && decimalValue <= 4095
            && decimalValue != 2048; // Exclude "800"
    }
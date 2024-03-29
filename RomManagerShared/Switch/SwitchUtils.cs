﻿using RomManagerShared.Base;namespace RomManagerShared.Switch;

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
        Dictionary<string, List<Rom>> romGroups = [];        foreach (var rom in romList)
        {
            if (string.IsNullOrEmpty(rom.TitleID))
                continue;            string modifiedTitleId = GetIdentifyingTitleID(rom.TitleID);            if (!romGroups.ContainsKey(modifiedTitleId))
            {
                romGroups[modifiedTitleId] = [];
            }            romGroups[modifiedTitleId].Add(rom);
        }
        List<List<Rom>> groupedRomList = new(
  romGroups.Values.Select(group => new List<Rom>(
      group.OrderBy(rom => rom is Game)
  ))
);
        return groupedRomList;
    }    public static string GetIdentifyingTitleID(string titleId)
    {
        return titleId[..^3];
    }    public static bool IsHexBetween001AndFFF(string hexValue)
    {
        int decimalValue;
        return int.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber, null, out decimalValue)
            && decimalValue >= 1
            && decimalValue <= 4095
            && decimalValue != 2048; // Exclude "800"
    }}

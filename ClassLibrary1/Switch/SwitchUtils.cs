using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RomManagerShared.Switch
{
    public class SwitchUtils
    {
        public static Type? GetRomMetadataClass(string titleId)
        {
            if (titleId.EndsWith("000") && titleId.Length == 16)
            {
                return typeof(SwitchGameMetaData);
            }
            else if (titleId.EndsWith("800") && titleId.Length == 16)
            {
                return typeof(SwitchUpdateMetaData);
            }
            else if (titleId.Length == 16 && IsHexBetween001AndFFF(titleId.Substring(13, 3)) && !titleId.Substring(13, 3).Equals("800", StringComparison.OrdinalIgnoreCase))
            {
                return typeof(SwitchDLCMetaData);
            }
            else
            {
                Console.WriteLine($"Unknown title ID format: {titleId}");
                return null;
            }
        }
        public static List<List<IRom>> GroupRomList(List<IRom> RomList)
        {
            Dictionary<string, List<IRom>> romGroups = new Dictionary<string, List<IRom>>();

            foreach (var rom in RomList)
            {
                string modifiedTitleId = GetIdentifyingTitleID(rom.TitleID);

                if (!romGroups.ContainsKey(modifiedTitleId))
                {
                    romGroups[modifiedTitleId] = new List<IRom>();
                }

                romGroups[modifiedTitleId].Add(rom);
            }

            List<List<IRom>> groupedRomList = romGroups.Values.ToList();

            return groupedRomList;
        }

        private static string GetIdentifyingTitleID(string titleId)
        {
            return titleId.Substring(0, titleId.Length - 3);
        }

        public static bool IsHexBetween001AndFFF(string hexValue)
        {
            int decimalValue;
            return int.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber, null, out decimalValue)
                && decimalValue >= 1
                && decimalValue <= 4095
                && decimalValue != 2048; // Exclude "800"
        }

    }
}

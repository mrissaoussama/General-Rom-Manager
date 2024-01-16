using RomManagerShared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RomManagerShared.PS4
{
    public class PS4Utils
    {
        static byte[] PKG_Magic = [127, 67, 78, 84];

        public static bool IsPS4PKG(string path)
        {
            FileStream pkgstream = new FileStream(path, FileMode.Open, FileAccess.Read);

            using (BinaryReader binaryReader = new BinaryReader(pkgstream))
            {
                byte[] a = binaryReader.ReadBytes(4);
                if (!BinUtils.CompareBytes(a, PKG_Magic))
                {
                    return false;
                }
                return true;
            }
        }

        public static string SystemFirmwareLongToString(long value)
        {
            string hexOutput = String.Format("{0:X}", value);
            if (value != 0)
            {
                string first_three = hexOutput.Substring(0, 3);
                return first_three.Insert(1, ".");
            }
            else
                return "0";
        }

        public static string GetTitleIDFromProductCode(string? productCode)
        {
            int startIndex = productCode.IndexOf('-') + 1;
            int endIndex = productCode.IndexOf('_', startIndex);
                var titleid= productCode[startIndex..endIndex];
            return titleid;        }
    }
}

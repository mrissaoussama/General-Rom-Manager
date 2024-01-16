using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using RomManagerShared.Utils;
using RomManagerShared.GameBoy;
using RomManagerShared.GameBoyAdvance;
using RomManagerShared.Base;
namespace RomManagerShared.DS
{
    public static class DSUtils
    {
        private static readonly string[] Extensions = ["nds"];

        private const int NintendoLogoOffset = 0xC0;

        public static bool IsDSRom(string filePath,bool checkExtensionOnly=false)
        {
            string fileName = Path.GetFileName(filePath);
            string extension = fileName.Substring(fileName.LastIndexOf(".") + 1);

            if (!Extensions.Contains(extension) && checkExtensionOnly==true)
            {
                return false;
            }

            byte[] buf = new byte[NintendoLogoOffset + GameBoyAdvanceUtils.GameBoyAdvanceNintendoLogo.Length];

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                int readBytes = fileStream.Read(buf, 0, buf.Length);
                if (readBytes != NintendoLogoOffset + GameBoyAdvanceUtils.GameBoyAdvanceNintendoLogo.Length)
                {
                    return false;
                }
            }
            return BinUtils.CompareBytes(buf, GameBoyAdvanceUtils.GameBoyAdvanceNintendoLogo);

        }
        public static string GetGameType(char firstCharacter)
        {
            return firstCharacter switch
            {
                'A' or 'B' or 'C' => "NDS common",
                'D' => "DSi-exclusive",
                'H' => "DSiWare (system utilities and browser)",
                'I' => "NDS and DSi-enhanced games with built-in Infrared port",
                'K' => "DSiWare (dsiware games and flipnote)",
                'N' => "NDS Nintendo Channel demo's Japan",
                'T' or 'Y' => "NDS many games",
                'U' => "NDS and DSi uncommon extra hardware",
                'V' => "DSi-enhanced games",
                _ => "Unknown category",
            };
        }
        public static void GetRegionAndLanguage(Rom DSrom)
        {
            char lastCharacter = DSrom.TitleID[3];
            switch (lastCharacter)
            {
                case 'J':
                    DSrom.Region = "Japan";
                    DSrom.Languages.Add("JP");
                    break;
                case 'P':
                    DSrom.Region = "Europe";
                    break;
                case 'F':
                    DSrom.Region = "French";
                    DSrom.Languages.Add("FR");
                    break;
                case 'S':
                    DSrom.Region = "Spain";
                    DSrom.Languages.Add("ES");
                    break;
                case 'E':
                    DSrom.Region = "USA";
                    DSrom.Languages.Add("EN");
                    break;
                case 'D':
                    DSrom.Region = "Germany";
                    DSrom.Languages.Add("DE");
                    break;
                case 'I':
                    DSrom.Region = "Italy";
                    DSrom.Languages.Add("IT");
                    break;
                case 'A':
                    DSrom.Region = "Asian";
                    break;
                case 'B':
                    DSrom.Region = "N/A";
                    break;
                case 'C':
                    DSrom.Region = "Chinese";
                    DSrom.Languages.Add("CN");
                    break;

                case 'K':
                    DSrom.Region = "Korean";
                    DSrom.Languages.Add("KR");
                    break;
                case 'M':
                    DSrom.Region = "Swedish";
                    DSrom.Languages.Add("SV");
                    break;
                case 'Q':
                    DSrom.Region = "Danish";
                    DSrom.Languages.Add("DA");
                    break;
                case 'U':
                    DSrom.Region = "Australia";
                    break;

                case 'N':
                    DSrom.Region = "Norwegia";
                    DSrom.Languages.Add("NN");
                    break;
                case 'R':
                    DSrom.Region = "Russia";
                    DSrom.Languages.Add("RU");
                    break;
                case 'V':
                    DSrom.Region = "EUR+AUS";
                    break;
                case 'W':
                case 'X':
                case 'Y':
                case 'Z':
                    DSrom.Region = "Europe";
                    break;
                case 'L':
                    DSrom.Languages.Add("EN");
                    break;
                case 'T':
                    DSrom.Region = "USA+AUS";
                    DSrom.Languages.Add("EN");
                    break;
                default:
                    break;
            }

        }
    }
    

}

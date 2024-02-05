using RomManagerShared.Base;
using RomManagerShared.GameBoyAdvance;
using RomManagerShared.Utils;
namespace RomManagerShared.DS
{
    public static class DSUtils
    {
        private static readonly string[] Extensions = ["nds"];        private const int NintendoLogoOffset = 0xC0;        public static bool IsDSRom(string filePath, bool checkExtensionOnly = false)
        {
            string fileName = Path.GetFileName(filePath);
            string extension = fileName.Substring(fileName.LastIndexOf(".") + 1);            if (!Extensions.Contains(extension) && checkExtensionOnly == true)
            {
                return false;
            }            byte[] buf = new byte[NintendoLogoOffset + GameBoyAdvanceUtils.GameBoyAdvanceNintendoLogo.Length];            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                int readBytes = fileStream.Read(buf, 0, buf.Length);
                if (readBytes != NintendoLogoOffset + GameBoyAdvanceUtils.GameBoyAdvanceNintendoLogo.Length)
                {
                    return false;
                }
            }
            return BinUtils.CompareBytes(buf, GameBoyAdvanceUtils.GameBoyAdvanceNintendoLogo);        }
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
                    DSrom.AddRegion(Region.Japan);
                    DSrom.AddLanguage(Language.Japanese);
                    break;
                case 'P':
                    DSrom.AddRegion(Region.Europe);
                    break;
                case 'F':
                    DSrom.AddRegion(Region.France);
                    DSrom.AddLanguage(Language.French);
                    break;
                case 'S':
                    DSrom.AddRegion(Region.Spain);
                    DSrom.AddLanguage(Language.Spanish);
                    break;
                case 'E':
                    DSrom.AddRegion(Region.USA);
                    DSrom.AddLanguage(Language.English);
                    break;
                case 'D':
                    DSrom.AddRegion(Region.Germany);
                    DSrom.AddLanguage(Language.German);
                    break;
                case 'I':
                    DSrom.AddRegion(Region.Italy);
                    DSrom.AddLanguage(Language.Italian);
                    break;
                case 'A':
                    DSrom.AddRegion(Region.Asia);
                    break;
                case 'B':
                    DSrom.AddRegion(Region.Unknown);
                    break;
                case 'C':
                    DSrom.AddRegion(Region.China);
                    DSrom.AddLanguage(Language.Chinese);
                    break;
                case 'K':
                    DSrom.AddRegion(Region.Korea);
                    DSrom.AddLanguage(Language.Korean);
                    break;
                case 'M':
                    DSrom.AddRegion(Region.Sweden);
                    DSrom.AddLanguage(Language.Swedish);
                    break;
                case 'Q':
                    DSrom.AddRegion(Region.Denmark);
                    DSrom.AddLanguage(Language.Danish);
                    break;
                case 'U':
                    DSrom.AddRegion(Region.Australia);
                    break;
                case 'N':
                    DSrom.AddRegion(Region.Norwegia);
                    DSrom.AddLanguage(Language.Norwegian);
                    break;
                case 'R':
                    DSrom.AddRegion(Region.Russia);
                    DSrom.AddLanguage(Language.Russian);
                    break;
                case 'V':
                    DSrom.AddRegion(Region.Australia);
                    DSrom.AddRegion(Region.Europe);
                    break;
                case 'W':
                case 'X':
                case 'Y':
                case 'Z':
                    DSrom.AddRegion(Region.Europe);
                    break;
                case 'L':
                    DSrom.AddLanguage(Language.English);
                    break;
                case 'T':
                    DSrom.AddRegion(Region.USA);
                    DSrom.AddRegion(Region.Australia);
                    DSrom.AddLanguage(Language.English);
                    break;
                default:
                    break;
            }
        }    }}

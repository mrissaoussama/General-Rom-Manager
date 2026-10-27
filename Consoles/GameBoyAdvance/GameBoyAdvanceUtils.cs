using RomManagerShared.Utils;
namespace RomManagerShared.GameBoyAdvance;

public static class GameBoyAdvanceUtils
{
    public static byte[] GameBoyAdvanceNintendoLogo => NintendoLogo.GameBoyAdvance;
    private const int NintendoLogoOffset = 4;
    public static bool IsGameBoyAdvanceRom(string filePath, bool checkExtensionOnly = false)
    {
        string fileName = Path.GetFileName(filePath);
        string extension = fileName[(fileName.LastIndexOf(".") + 1)..];
        if (!Extensions.Contains(extension) && checkExtensionOnly == true)
        {
            return false;
        }
        byte[] buf = new byte[NintendoLogoOffset + GameBoyAdvanceNintendoLogo.Length];
        using (FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read))
        {
            int readBytes = fileStream.Read(buf, 0, buf.Length);
            if (readBytes != NintendoLogoOffset + GameBoyAdvanceNintendoLogo.Length)
            {
                return false;
            }
        }
        return BinUtils.CompareBytes(buf, GameBoyAdvanceNintendoLogo);
    }
    private static readonly string[] Extensions = ["gba", "agb"];
}

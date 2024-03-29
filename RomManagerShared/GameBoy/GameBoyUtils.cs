﻿using RomManagerShared.Utils;
namespace RomManagerShared.GameBoy;

public static class GameBoyUtils
{
    private static readonly byte[] GameBoyNintendoLogo = {
    0xCE, 0xED, 0x66, 0x66, 0xCC, 0x0D, 0x00, 0x0B, 0x03, 0x73, 0x00, 0x83, 0x00, 0x0C, 0x00, 0x0D,
    0x00, 0x08, 0x11, 0x1F, 0x88, 0x89, 0x00, 0x0E, 0xDC, 0xCC, 0x6E, 0xE6, 0xDD, 0xDD, 0xD9, 0x99,
    0xBB, 0xBB, 0x67, 0x63, 0x6E, 0x0E, 0xEC, 0xCC, 0xDD, 0xDC, 0x99, 0x9F, 0xBB, 0xB9, 0x33, 0x3E
};    private const int NintendoLogoOffset = 260; // 0x104 - 0x133
    public static bool IsGameBoyRom(string filePath, bool checkExtensionOnly = false)
    {
        string fileName = Path.GetFileName(filePath);
        string extension = fileName[(fileName.LastIndexOf(".") + 1)..];        if (!Extensions.Contains(extension) && checkExtensionOnly == true)
        {
            return false;
        }        byte[] buf = new byte[NintendoLogoOffset + GameBoyNintendoLogo.Length];        using (FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read))
        {
            int readBytes = fileStream.Read(buf, 0, buf.Length);
            if (readBytes != NintendoLogoOffset + GameBoyNintendoLogo.Length)
            {
                return false;
            }
        }
        return BinUtils.CompareBytes(buf, GameBoyNintendoLogo);    }    private static readonly string[] Extensions = ["gb"];
}
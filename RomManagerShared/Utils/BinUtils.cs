﻿using System.Security.Cryptography;
using System.Text;

public static class BinUtils
{
    public static string AsciiToString(byte[] data, int offset, int length)
    {
        return Encoding.ASCII.GetString(data, offset, length).Trim();
    }
    /// <summary>
    /// Adds "0x" and removes "-"
    /// </summary>
    /// <param name="buff"></param>
    /// <returns></returns>
    public static string ByteArrayToPrefixedString(byte[] buff)
    {
        return "0x" + BitConverter.ToString(buff).Replace("-", "");
    }
    /// <summary>
    /// returns a string with hyphens
    /// </summary>
    /// <param name="buff"></param>
    /// <returns></returns>
    public static string ByteToString(byte[] buff)
    {
        return BitConverter.ToString(buff);
    }
    {
        return value.ToString("X2");
    }
    /// <summary>
    /// returns true if 2 byte arrays with equal length have the same values
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool CompareBytes(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }
        {
            if (a[i] != b[i])
            {
                return false;
            }
        }
    }
    //        public static void SaveThumbnail(SwitchGame nspMetaData, string filePath)
    //        {
    //            using (Image thumbnailImage = Base64StringToImage(nspMetaData.Thumbnail))
    //            {
    //                var thumbnailPath = filePath + ".jpg";
    //                Console.WriteLine($"Saved thumbnail to: {thumbnailPath}!");
    //#pragma warning disable CA1416 // Validate platform compatibility
    //                thumbnailImage.Save(thumbnailPath);
    //#pragma warning restore CA1416 // Validate platform compatibility
    //            }
    //        }
    //        public static Image Base64StringToImage(string input)
    //        {
    //            var bytes = Convert.FromBase64String(input);
    //            var stream = new MemoryStream(bytes);
    //#pragma warning disable CA1416 // Validate platform compatibility
    //            return Image.FromStream(stream);
    //#pragma warning restore CA1416 // Validate platform compatibility
    //        }
    /// <summary>
    /// returns a SH1256 hash byte array
    /// </summary>
    /// <param name="ba"></param>
    /// <returns></returns>
    public static byte[] SHA256Bytes(byte[] ba)
    {
        SHA256 mySHA256 = SHA256.Create();
        byte[] hashValue;
        hashValue = mySHA256.ComputeHash(ba);
        return hashValue;
    }
    public static byte[] ReadBytes(string filePath, int offset, int length)
    {
        byte[] bytes;
        {
            bytes = new byte[length];
            fileStream.Seek(offset, SeekOrigin.Begin);
            fileStream.Read(bytes, 0, length);
        }
    }
    public static string ReadUnicodeString(string filePath, int offset, int length)
    {
        byte[] unicodeBytes;
        {
            unicodeBytes = new byte[length * 2];
            fileStream.Seek(offset, SeekOrigin.Begin);
            fileStream.Read(unicodeBytes, 0, length * 2);
        }
        string unicodeString = Encoding.Unicode.GetString(unicodeBytes);
        int nullCharIndex = unicodeString.IndexOf('\0');
        if (nullCharIndex != -1)
        {
            unicodeString = unicodeString[..nullCharIndex];
        }
    }
}
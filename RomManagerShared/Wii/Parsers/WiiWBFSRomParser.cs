﻿using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using System.Text;
namespace RomManagerShared.Wii.Parsers;

public class WiiWBFSRomParser : IRomParser<WiiConsole>
{
    public List<string> Extensions { get; set; }
    private const int TitleIDLength = 6;
    private const int TitleNameOffset = 0x220;
    private const int TitleNameLength = 160;
    private const int MD5HashOffset = 0x2EC;
    private const int MD5HashLength = 0x10;
    public WiiWBFSRomParser()
    {
        Extensions = ["wbfs"];
    }
    {
        List<Rom> roms = [];
        {
            using FileStream fs = new(path, FileMode.Open, FileAccess.Read);
            using BinaryReader br = new(fs);
            // Read and process the WBFS file
            byte[] titleIDBytes = new byte[TitleIDLength];
            byte[] titleNameBytes = new byte[TitleNameLength];
            byte[] md5HashBytes = new byte[MD5HashLength];
            br.BaseStream.Seek(TitleIDOffset, SeekOrigin.Begin);
            br.Read(titleIDBytes, 0, TitleIDLength);
            string titleID = Encoding.ASCII.GetString(titleIDBytes);
            br.BaseStream.Seek(TitleNameOffset, SeekOrigin.Begin);
            br.Read(titleNameBytes, 0, TitleNameLength);
            string titleName = Encoding.ASCII.GetString(titleNameBytes);
            //// DOES NOT MATCH ACTUAL FILE MD5, probably the hash of the actual game data, i don't know since i didn't find a documentation of the format
            //br.BaseStream.Seek(MD5HashOffset, SeekOrigin.Begin);
            //br.Read(md5HashBytes, 0, MD5HashLength);
            //string md5Hex = BitConverter.ToString(md5HashBytes).Replace("-", "").ToLower();
            //Console.WriteLine(md5Hex);
            WiiGame wiiRom = new()
            {
                TitleID = titleID.Trim(),
                //Hash = md5HashBytes,
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing WBFS file: {ex.Message}");
        }
    }
}
﻿namespace RomManagerShared.GameBoyAdvance;

public class GameBoyAdvanceMetadata
{
    public string GameCode { get; set; }
    public string MakerCode { get; set; }
    public string UnitCode { get; set; }
    public string VersionCode { get; set; }
    public string HeaderChecksum { get; set; }
    {
        return GameCode.Length == 4 ? GameCode[0].ToString() : "";
    }
    {
        return GameCode.Length == 4 ? GameCode[3..] : "";
    }
}
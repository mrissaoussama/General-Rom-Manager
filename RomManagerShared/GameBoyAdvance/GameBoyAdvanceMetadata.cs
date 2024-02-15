namespace RomManagerShared.GameBoyAdvance;

public class GameBoyAdvanceMetadata
{    public string Title { get; set; }
    public string GameCode { get; set; }
    public string MakerCode { get; set; }
    public string UnitCode { get; set; }
    public string VersionCode { get; set; }
    public string HeaderChecksum { get; set; }    public string GetGameTypeChar()
    {
        return GameCode.Length == 4 ? GameCode[0].ToString() : "";
    }    public string GetCountryChar()
    {
        return GameCode.Length == 4 ? GameCode[3..] : "";
    }
}

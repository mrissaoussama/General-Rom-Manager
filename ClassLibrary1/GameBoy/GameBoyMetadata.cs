namespace RomManagerShared.GameBoy
{
    public class GameBoyMetadata
    {
        public string Title { get; set; }
        public string GameCode { get; set; }
        public string CgbFlag { get; set; }
        public string NewLicenseeCode { get; set; }
        public string SgbFlag { get; set; }
        public string CartridgeType { get; set; }
        public string RomSize { get; set; }
        public string RamSize { get; set; }
        public string DestinationCode { get; set; }
        public string OldLicenseeCode { get; set; }
        public string MaskRomVersionNumber { get; set; }
        public string HeaderChecksum { get; set; }
        public byte[] StoredGlobalChecksum { get; internal set; }
        public string CgbFlagcode { get; internal set; }        public string GetGameTypeChar()
        {
            if (GameCode.Length == 4)
            {
                return GameCode[0].ToString();
            }
            return "";
        }        public string GetCountryChar()
        {
            if (GameCode.Length == 4)
            {
                return GameCode.Substring(3);
            }
            return "";
        }
    }
}

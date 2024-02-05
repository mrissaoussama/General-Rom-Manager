namespace RomManagerShared.DS
{
    public class DSMetadata
    {
        public string Title { get; set; }
        public string GameCode { get; set; }
        public string MakerCode { get; set; }
        public string UnitCode { get; set; }
        public string RegionCode { get; internal set; }
        public string RomVersion { get; internal set; }
        public int IconOffset { get; internal set; }
        public string IconBitmap { get; internal set; }
        public string[] Titles { get; internal set; }
    }
}

namespace RomManagerShared.Base
{
    public abstract class Rom
    {
        public string TitleID { get; set; }
        public string Version { get; set; }
        public string Region { get; set; }
        public string Icon { get; set; }
        public string Rating { get; set; }
        public string Publisher { get; set; }
        public string Thumbnail { get; set; }

        public List<string> RatingContent { get; set; }
        public List<string> Genres { get; set; }
        public List<string> Languages { get; set; }
        public string Path { get; set; }
        public string TitleName { get; set; }
        public string Developer { get; set; }
        public long Size { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string MinimumFirmware { get; set; }
        public bool IsDemo { get; set; }
        public int NumberOfPlayers { get; set; }
        public DateOnly ReleaseDate { get; set; }
        public string Banner { get; set; }
        public byte[] Hash { get; set; }
        public List<string> Images { get; set; }
        public string ProductCode { get; set; }
        public Rom()
        {
            TitleID = string.Empty;
            Version = string.Empty;
            Region = string.Empty;
            Icon = string.Empty;
            Rating = string.Empty;
            Publisher = string.Empty;
            Thumbnail = string.Empty;
            RatingContent = [];
            Genres = [];
            Languages = [];
            Path = string.Empty;
            TitleName = string.Empty;
            Developer = string.Empty;
            Size = 0;
            Type = string.Empty;
            Description = string.Empty;
            MinimumFirmware = string.Empty;
            IsDemo = false;
            NumberOfPlayers = 0;
            ReleaseDate = default(DateOnly);
            Banner = string.Empty;
            Hash = Array.Empty<byte>(); 
            Images = [];
            ProductCode = string.Empty;
        }
        public override string ToString()
        {
            return $"{TitleID}| {TitleName} | {Version}";
        }
    }
    public abstract class DLC : Rom
    {
        public DLC() : base()
        {
        }
        public string MinimumGameUpdate { get; set; } = "";
    }
    public abstract class Game : Rom
    {
        //maybe add supported controllers if possible
        public Game() : base()
        {
        }
    }
    public abstract class Update : Rom
    {
        public Update() : base()
        {
        }
    }
}
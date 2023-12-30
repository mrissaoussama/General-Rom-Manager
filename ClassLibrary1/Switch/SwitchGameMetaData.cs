using RomManagerShared;

namespace RomManagerShared.Switch
{
    public class SwitchGameMetaData:IGame
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string TitleID { get; set; }

        public string TitleName { get; set; }

        public string Path { get; set; }
        public string Developer { get; set; }
        public string Thumbnail { get; set; }
        public string Description { get; set; }
        public long Size { get; set; }
        public string Type { get; set; }
        public string Version { get; set; }
        public string Region { get; set; }
        public string MinimumFirmware { get; set; }
        public List<string> Images { get; set; }
        public bool IsDemo { get; set; }
        public int NumberOfPlayers { get; set; }
        public DateOnly ReleaseDate { get; set; }
        public string Icon { get ; set ; }
        public int Rating { get ; set ; }
        public string Publisher { get ; set ; }
        public List<string> RatingContent { get ; set ; }
        public List<string> Genres { get ; set ; }
        public List<string> Languages { get ; set ; }
        public string Banner { get ; set ; }
        public string ProductCode { get; set; }

        public byte[] Hash { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    }
}

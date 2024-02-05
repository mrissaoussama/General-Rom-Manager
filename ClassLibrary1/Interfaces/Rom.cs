
using RomManagerShared.Utils;

namespace RomManagerShared.Base
{
    public  class Rom
    {
        public string? TitleID { get; set; }
        public string? Version { get; set; }
        public HashSet<Region>? Regions { get; set; }
        public string? Icon { get; set; }
        public HashSet<Rating>? Ratings { get; set; }
        public string? Publisher { get; set; }
        public string? Thumbnail { get; set; }
        public HashSet<RomTitle>? Titles { get; set; }
        public HashSet<Language>? Languages { get; set; }
        public HashSet<GenreEnum>? Genres { get; set; }
        public string? Path { get; set; }
        public HashSet<RomDescription>? Descriptions { get; set; }
        public string? Developer { get; set; }
        public long? Size { get; set; }
        public List<RomHash>? Hashes { get; set; }
        public DateOnly? ReleaseDate { get; set; }
        public string? Banner { get; set; }
        public HashSet<string>? Images { get; set; }
        public string? ProductCode { get;  set; }
        public string? MinimumFirmware { get;  set; }
        public int NumberOfPlayers { get;  set; }
        public bool IsDemo { get;  set; }
        public bool IsFolderFormat { get; set; }
        public HashSet<Game> RomHacks { get;  set; }
        public HashSet<Update> Updates { get;  set; }
        public HashSet<DLC> DLCs { get;  set; }
        public Rom()
        {
        }
        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            Rom otherRom = (Rom)obj;
            return otherRom.TitleID == TitleID;
        }
        public void AddTitleName(string title, Language Language = Language.Unknown)
        {
            Titles ??= [];
            Titles.Add(new RomTitle { Value = title, Language = Language });
        }
        public void AddLanguage(Language l)
        {
            Languages ??= [];
            Languages.Add(l);
        }
        public void AddRegion(Region r)
        {
            Regions ??= [];
            Regions.Add(r);
        }
        public void AddRegion(string r)
        {
            Regions ??= [];
            var reg = RegionHelper.ParseRegion(r);
            Regions.Add(reg);
        }
        public override string ToString()
        {
            return $"{TitleID}| {Titles?.First().Value} | {Version}";
        }
        public void AddDescription(string description, Language Language = Language.Unknown)
        {
            if (Descriptions is null) Descriptions = [];
            Descriptions.Add(new RomDescription(description, Language));
        }
        internal void AddRating(Rating r)
        {
            Ratings ??= [];
            Ratings.Add(r);
        }
        internal void AddImage(string pathOrUrl)
        {
            Images ??= [];
            Images.Add(pathOrUrl);
        }
    }
    public class RomDescription
    {
        public Language Language { get; set; }
        public string Value { get; set; }
        public static implicit operator RomDescription(string titleValue)
        {
            return new RomDescription { Value = titleValue };
        }
        public override string ToString()
        {
            return $"{Value} [{Language}]";
        }
        public RomDescription(string description, Language Language = Language.Unknown)
        {
            this.Language = Language;
            this.Value = description;
        }        public RomDescription()
        {
        }
    }
    public class RomHash
    {
        public HashTypeEnum Type { get; set; }
        public  string Value { get; set; }
        public DateTime CreationDate { get; set; }
        //thinking about adding a source enum for local/external to know the origin of the hash,
        //but description might be better idk
        public string? Description { get; set; }
        public bool IsVerified { get; set; }
        public override string ToString()
        {
            return $"{Type} ({Value}) {Description} {CreationDate} {IsVerified}";
        }
    }
    //public enum HashTrustLevel
    //{
    //    UnknownOrCorrupted,
    //    Verified
    //}
    public enum HashTypeEnum
    {
        CRC32,
        MD5,
        SHA1, SHA256
    }
    public enum Region
    {
        Unknown,        USA,
        Europe,
        Japan,
        Asia,
        HongKong,
        France,
        Italy,
        Germany,
        Spain,
        Korea,
        Australia,
        China,
        Taiwan,
        Sweden,
        Denmark,
        Norwegia,
        Russia,
        Brazil,
        Netherlands,
        Canada,
        Scandinavia
    }

    public enum RatingContent
    {
        AlcoholReference, Blood,
        ComicMischief, DrugReference,
        GamblingThemes, Language,
        MatureHumor, PartialNudity,
        SexualContent, SexualViolence,
        StrongLanguage, StrongSexualContent, InGamePurchases, MildBlood,
        TobaccoReference, UseofDrugs, UseofAlcoholandTobacco,
        Violence, AnimatedBlood, BloodandGore,
        CrudeHumor, FantasyViolence, IntenseViolence, UsersInteract, UseofDrugsandAlcohol,
        Lyrics, Nudity, RealGambling, SexualThemes, MildSuggestiveThemes, MildFantasyViolence,
        SimulatedGambling, StrongLyrics, SuggestiveThemes,
        UseofAlcohol, UseofTobacco, ViolentReferences, CartoonViolence, MildViolence, MildLanguage
    }
  
    public enum GenreEnum
    {
        Unknown,
        Adventure,
        Fighting,
        Puzzle
    }
    public enum RatingSystem
    {
        Unknown, PEGI,
        ESRB,
        CERO
    }
    public class RomTitle
    {
        public Language Language { get; set; }
        public string Value { get; set; }
        public static implicit operator RomTitle(string titleValue)
        {
            return new RomTitle { Value = titleValue };
        }
        public override string ToString()
        {
            return $"{Value} [{Language}]";
        }
        public RomTitle(string title, Language Language = Language.Unknown)
        {
            this.Language = Language;
            this.Value = title;
        }        public RomTitle()
        {
        }
    }
    public class Rating
    {
        public RatingSystem Name { get; set; }
        public int Age { get; set; }
        public HashSet<RatingContent> RatingContents { get; set; } = [];

        public void AddContent(List<string> ratingContent)
        {
            foreach (var content in ratingContent)
            {
                AddContent(content);
            }
        }
        public void AddContent(string ratingContent)
        {
            ratingContent = ratingContent.Replace(" ", "").Replace("-", "");
            try
            {
                AddContent(Enum.Parse<RatingContent>(ratingContent));

            }
            catch (ArgumentException ex)
            {
                FileUtils.Log($"Rating Content {ratingContent} not found");
            }
        }
        public void AddContent(RatingContent content)
        {
            RatingContents ??= [];
            RatingContents.Add(content);
        }
    }
    public  class DLC : Rom
    {
        public DLC() : base()
        {
        }
        public string MinimumGameUpdate { get; set; } = "";
        public Game? RelatedGame { get; set; }
    }
    public  class Game : Rom
    {
        //maybe add supported controllers if possible
        public Game() : base()
        {
        }
    }
    public  class Update : Rom
    {
        public Game? RelatedGame { get; set; }

        public Update() : base()
        {
        }
    }
    public class Homebrew : Rom
    {
    }

}
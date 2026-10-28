
using System.Reflection;
using RomManagerShared.Utils;

namespace RomManagerShared.Base;
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class UiPropertyAttribute : Attribute
{
    public bool Visible { get; set; } = true;
    public string DisplayName { get; set; } = "";
    public int Order { get; set; } = 100;
    public string Format { get; set; } = "";
    public UiPropertyType PropertyType { get; set; } = UiPropertyType.Auto;
    
    public UiPropertyAttribute() { }
    
    public UiPropertyAttribute(bool visible)
    {
        Visible = visible;
    }
    
    public UiPropertyAttribute(string displayName)
    {
        DisplayName = displayName;
    }
}

public enum UiPropertyType
{
    Auto,
    Text,
    Number,
    FileSize,
    Date,
    Boolean,
    Enum,
    List,
    Image,
    Path,
    Region,
    Language,
    URL,
    Hash,
    Rating,
    Genre,
    Description,
    Title,
}
public class Rom
{
        [UiProperty(false)]
    public int Id { get; set; }
    [UiProperty(DisplayName = "Title ID", Order = 1)]
    public string? TitleID { get; set; }
    [UiProperty(DisplayName = "Version", Order = 2)]

    public string? Version { get; set; }

    [UiProperty(PropertyType = UiPropertyType.List)]
    
    public  List<Region>? Regions { get; set; }
    [UiProperty(PropertyType = UiPropertyType.Image)]

    public string? Icon { get; set; }
    [UiProperty(PropertyType = UiPropertyType.List)]

    public virtual List<Rating>? Ratings { get; set; }
    public string? Publisher { get; set; }
    public string? Thumbnail { get; set; }
    public virtual List<RomTitle>? Titles { get; set; }
    public List<Language>? Languages { get; set; }
    public List<GenreEnum>? Genres { get; set; }
    public string? Path { get; set; }
    public virtual List<RomDescription>? Descriptions { get; set; }
    public string? Developer { get; set; }
    public long? Size { get; set; }
    public virtual List<RomHash>? Hashes { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public string? Banner { get; set; }
    public List<string>? Images { get; set; }
    public string? ProductCode { get; set; }
    public string? MinimumFirmware { get; set; }
    public int NumberOfPlayers { get; set; }
    public bool IsDemo { get; set; }
    public bool IsFolderFormat { get; set; }

    public Rom()
    {
    }
    public override bool Equals(object? obj)
    {
        if (obj is null || obj is not Rom otherRom) return false;
        return otherRom.TitleID == TitleID && otherRom.Path == Path;
    }

    public override int GetHashCode() => HashCode.Combine(TitleID, Path);
    public void AddTitleName(string title, Language Language = Language.Unknown)
    {
        Titles ??= [];
        if (string.IsNullOrWhiteSpace(title)) return;
        title=title.ReplaceLineEndings(" ");
        if (Titles.FirstOrDefault(x => x.Value == title) is not null)
            return;
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
        var title = Titles?.FirstOrDefault()?.Value ?? "Unknown";
        var region = Regions?.FirstOrDefault().ToString() ?? "Unknown";
        return $"{TitleID}| {title} |{region}| {Version}";
    }
    public void AddDescription(string description, Language Language = Language.Unknown)
    {
        Descriptions ??= [];
        Descriptions.Add(new RomDescription(description, Language));
    }
    public void AddRating(Rating r)
    {
        Ratings ??= [];
        Ratings.Add(r);
    }
    public void AddImage(string pathOrUrl)
    {
        Images ??= [];
        Images.Add(pathOrUrl);
    }
    public void AddImages(IEnumerable<string> images)
    {
        Images ??= [];
        Images.AddRange(images);
    }

    /// <summary>
    /// Fills this rom with what <paramref name="other"/> knows, e.g. when two parsers read the same title.
    /// Values from <paramref name="other"/> win when set; lists are combined without duplicates; flags are
    /// OR-ed (an unset <c>false</c> never clears them). <see cref="Id"/> is kept. Properties that subclasses
    /// add are copied when <paramref name="other"/> has them set and this type has them too.
    /// </summary>
    public void MergeFrom(Rom other)
    {
        static string? Pick(string? mine, string? theirs) => string.IsNullOrWhiteSpace(theirs) ? mine : theirs;
        static List<TItem>? Union<TItem, TKey>(List<TItem>? mine, List<TItem>? theirs, Func<TItem, TKey> key)
        {
            if (theirs is null || theirs.Count == 0) return mine;
            mine ??= [];
            var seen = mine.Select(key).ToHashSet();
            foreach (var item in theirs)
                if (seen.Add(key(item))) mine.Add(item);
            return mine;
        }

        TitleID = Pick(TitleID, other.TitleID);
        Version = Pick(Version, other.Version);
        Icon = Pick(Icon, other.Icon);
        Publisher = Pick(Publisher, other.Publisher);
        Thumbnail = Pick(Thumbnail, other.Thumbnail);
        Path = Pick(Path, other.Path);
        Developer = Pick(Developer, other.Developer);
        Banner = Pick(Banner, other.Banner);
        ProductCode = Pick(ProductCode, other.ProductCode);
        MinimumFirmware = Pick(MinimumFirmware, other.MinimumFirmware);
        Size = other.Size ?? Size;
        ReleaseDate = other.ReleaseDate ?? ReleaseDate;
        if (other.NumberOfPlayers > 0) NumberOfPlayers = other.NumberOfPlayers;
        IsDemo |= other.IsDemo;
        IsFolderFormat |= other.IsFolderFormat;

        Regions = Union(Regions, other.Regions, r => r);
        Languages = Union(Languages, other.Languages, l => l);
        Genres = Union(Genres, other.Genres, g => g);
        Images = Union(Images, other.Images, i => i);
        Titles = Union(Titles, other.Titles, t => t.Value);
        Descriptions = Union(Descriptions, other.Descriptions, d => (d.Language, d.Value));
        Ratings = Union(Ratings, other.Ratings, r => r.Name);
        Hashes = Union(Hashes, other.Hashes, h => (h.Type, h.Value));

        foreach (var (from, to) in ExtraProperties(other.GetType(), GetType()))
        {
            var value = from.GetValue(other);
            if (value is not null && !(from.PropertyType.IsValueType && value.Equals(Activator.CreateInstance(from.PropertyType))))
                to.SetValue(this, value);
        }
    }

    // Properties declared below Rom on the source type, paired with a same-named writable property of the target.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(Type, Type), (PropertyInfo From, PropertyInfo To)[]> ExtraCache = new();

    private static (PropertyInfo From, PropertyInfo To)[] ExtraProperties(Type source, Type target) =>
        ExtraCache.GetOrAdd((source, target), key =>
        [
            .. key.Item1.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.DeclaringType != typeof(Rom) && p.CanRead && p.GetIndexParameters().Length == 0)
                .Select(p => (From: p, To: key.Item2.GetProperty(p.Name, BindingFlags.Public | BindingFlags.Instance)))
                .Where(x => x.To is { CanWrite: true } && x.To.PropertyType == x.From.PropertyType)
                .Select(x => (x.From, x.To!)),
        ]);
}
public class RomDescription
{
    public int Id { get; set; }
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
    }
    public RomDescription()
    {
        Value = string.Empty;

    }
}
public class RomHash
{
    public int Id { get; set; }
    //public Rom Rom { get; set; }
    public RomHash()
    {
        Value = string.Empty;
    }
    public void AddProperty(string key, string value)
    {
        Properties ??= [];
        var existingProperty = Properties.FirstOrDefault(p => p.Key == key);
        if (existingProperty != null)
        {
            existingProperty.Value = value;
        }
        else
        {
            Properties.Add(new RomHashProperty { Key = key, Value = value });
        }
    }

    public HashTypeEnum Type { get; set; }
    public string Value { get; set; }
    public DateTime CreationDate { get; set; }
    public string? Description { get; set; }
    public string? Extension { get; set; }
    public string? Filename { get; set; }
    public bool IsVerified { get; set; }
    public virtual List<RomHashProperty>? Properties { get; set; }
    public override string ToString()
    {
        return $"{Type} ({Value}) {Description} {CreationDate} {IsVerified}";
    }
}
public class RomHashProperty
{
    public int Id { get; set; }
    public  string Key { get; set; }= string.Empty;
    public  string Value { get; set; }= string.Empty;
}

public enum HashTypeEnum
{
    CRC32,
    MD5,
    SHA1, SHA256
}//public enum HashTrustLevel
//{
//    UnknownOrCorrupted,
//    Verified
//}
public enum Region
{
    Unknown,
    USA,
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
    Scandinavia,
    UnitedKingdom,
    World
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
    // Stored as numbers in the library cache: add new values at the end only.
    Unknown,
    Adventure,
    Fighting,
    Puzzle,
    Action,
    Platformer,
    Racing,
    RolePlaying,
    Shooter,
    Sports,
    Strategy,
    Simulation,
    Music,
    Party,
    Arcade,
    Casual,
    Educational,
    Family,
    Board,
    Card,
}
public enum RatingSystem
{
    Unknown, PEGI,
    ESRB,
    CERO
}
public class RomTitle
{
    public int Id { get; set; }
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
    }
    public RomTitle()
    {
        Value = string.Empty;
    }
}
public class Rating
{
    public int Id { get; set; }
    public RatingSystem Name { get; set; }
    public int Age { get; set; }
    public List<RatingContent> RatingContents { get; set; } = [];

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
        // Descriptors this enum does not know are skipped (title databases add new ones over time).
        if (Enum.TryParse<RatingContent>(ratingContent, out var content))
            AddContent(content);
    }
    public void AddContent(RatingContent content)
    {
        RatingContents ??= [];
        RatingContents.Add(content);
    }
}
public class DLC : Rom
{
    public DLC() : base()
    {
    }
    public string MinimumGameUpdate { get; set; } = "";
    /// <summary>The library's base game, linked after each scan (<c>RomRelations.Link</c>); not stored.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public virtual Game? RelatedGame { get; set; }
}
public class Game : Rom
{
    [System.Text.Json.Serialization.JsonIgnore]
    public virtual List<Game>? RomHacks { get; set; }
    /// <summary>The library's updates for this game, linked after each scan (<c>RomRelations.Link</c>); not stored.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public virtual List<Update>? Updates { get; set; }
    /// <summary>The library's DLC for this game, linked after each scan; not stored.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public virtual List<DLC>? DLCs { get; set; }
    //maybe add supported controllers if possible
    public Game() : base()
    {
    }
}
public class Update : Rom
{
    /// <summary>The library's base game, linked after each scan (<c>RomRelations.Link</c>); not stored.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public virtual Game? RelatedGame { get; set; }

    public Update() : base()
    {
    }
}
public class License
{
    public License(string path)
    {
        Path = path;
    }
    public string LicenseId { get; set; } = "";
    public Rom? Rom { get; set; }
    public string Path { get; set; }
    public string TitleID { get; set; } = "";
}
public class Homebrew : Rom
{
}

/// <summary>
/// For folder-format roms (<see cref="Rom.IsFolderFormat"/>): where the game folder is relative to the marker file
/// the parser opened, e.g. 2 for <c>&lt;game&gt;/code/app.xml</c>. Console modules implement it on their rom types.
/// </summary>
public interface IFolderLayout
{
    /// <summary>How many folders above the marker file the game folder is (1 = its folder); 0 when unknown.</summary>
    int FolderLevelsAboveMarker(string markerFileName);
}

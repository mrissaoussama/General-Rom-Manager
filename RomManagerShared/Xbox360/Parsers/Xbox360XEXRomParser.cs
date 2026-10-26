using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
using RomManagerShared.Utils.ISO2GOD;
using RomManagerShared.Xbox360.Configuration;
namespace RomManagerShared.Xbox360.Parsers;

public class Xbox360XEXRomParser : IRomParser<Xbox360Console>, IHasRequirements
{
    private readonly ConsoleRequirement _xexToolReq = new()
    {
        Id = "xbox360.xextool",
        DisplayName = "xextool.exe",
        Description = "Optional: adds names and icons. Title IDs are read without it. Set Xbox360:XexToolPath.",
        Kind = RequirementKind.FilePath,
        DefaultValue = Xbox360Configuration.GetXexToolPath(),
        IsOptional = true,
    };

    public Xbox360XEXRomParser()
    {
        Extensions = ["xex"];
    }
    public List<string> Extensions { get; set; }
    public IReadOnlyList<ConsoleRequirement> GetRequirements() => [_xexToolReq];

    public Task<List<Rom>> ProcessFile(string path)
    {
        List<Rom> list = [];
        IsoDetailsResults? results = null;
        try
        {

            IsoDetails isoDetails = new();
            results = IsoDetails.readXexFromFile(path);
            if (results is null)
                return Task.FromResult(list);
            Xbox360Game Xbox360rom = new();
            var name = results.Name?.RemoveTrailingNullTerminators();
            // Without xextool there is no name; the game folder usually carries it.
            Xbox360rom.AddTitleName(string.IsNullOrWhiteSpace(name) ? new DirectoryInfo(Path.GetDirectoryName(path)!).Name : name);
            Xbox360rom.TitleID = results.TitleID;
            if (results.RawThumbnail != null)
            {
                Xbox360rom.AddImage(SaveImageToPath(results.RawThumbnail, results.TitleID));
            }
            Xbox360rom.Size = FileUtils.GetFileSize(path);
            Xbox360rom.Path = path;
            Xbox360rom.IsFolderFormat = true;
            list.Add(Xbox360rom);
        }
        catch (Exception ex)
        {
            FileUtils.Log(ex.Message);
        }
        return Task.FromResult(list);

    }
    public static string SaveImageToPath(byte[] png, string titleid)
    {
        var xbox360cache = Xbox360Configuration.GetThumbnailCachePath();
        var titleidfolder = Path.Combine(xbox360cache, titleid);
        Directory.CreateDirectory(titleidfolder);
        var imagefilepath = Path.Combine(titleidfolder, "Thumbnail.png");
        File.WriteAllBytes(imagefilepath, png);
        return imagefilepath;
    }
}


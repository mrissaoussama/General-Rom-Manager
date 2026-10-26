using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.OriginalXbox.Configuration;
using RomManagerShared.Utils;
using RomManagerShared.Utils.ISO2GOD;
namespace RomManagerShared.OriginalXbox.Parsers;

public class OriginalXboxXBERomParser : IRomParser<OriginalXboxConsole>
{
    public OriginalXboxXBERomParser()
    {
        Extensions = ["xbe"];
    }
    public List<string> Extensions { get; set; }
    public Task<List<Rom>> ProcessFile(string path)
    {
        List<Rom> list = [];
        IsoDetailsResults? results = null;
        try
        {

            IsoDetails isoDetails = new();
            results = IsoDetails.readXbeFromFile(path);
            if (results is null)
                return Task.FromResult(list);
            OriginalXboxGame OriginalXboxrom = new();
            OriginalXboxrom.AddTitleName(results.Name.RemoveTrailingNullTerminators());
            OriginalXboxrom.TitleID = results.TitleID;
            OriginalXboxrom.IsFolderFormat = true;

            if (results.RawThumbnail != null)
            {
                OriginalXboxrom.AddImage(SaveImageToPath(results.RawThumbnail, results.TitleID));
            }
            OriginalXboxrom.Size = FileUtils.GetFileSize(path);
            OriginalXboxrom.Path = path;
            list.Add(OriginalXboxrom);
        }
        catch (Exception ex)
        {
            FileUtils.Log(ex.Message);
        }
        return Task.FromResult(list);

    }
    public static string SaveImageToPath(byte[] png, string titleid)
    {
        var xbox360cache = OriginalXboxConfiguration.GetThumbnailCachePath();
        var titleidfolder = Path.Combine(xbox360cache, titleid);
        Directory.CreateDirectory(titleidfolder);
        var imagefilepath = Path.Combine(titleidfolder, "Thumbnail.png");
        File.WriteAllBytes(imagefilepath, png);
        return imagefilepath;
    }
}


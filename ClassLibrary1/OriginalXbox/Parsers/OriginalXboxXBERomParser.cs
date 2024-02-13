using RomManagerShared.Base;
using RomManagerShared.Utils;
using RomManagerShared.Utils.ISO2GOD;
using System.Drawing;
namespace RomManagerShared.OriginalXbox.Parsers;

public class OriginalXboxXBERomParser : IRomParser
{
    public OriginalXboxXBERomParser()
    {
        Extensions = ["xbe"];
    }
    public HashSet<string> Extensions { get; set; }
    public Task<HashSet<Rom>> ProcessFile(string path)
    {
        HashSet<Rom> list = [];
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
            if (results.Thumbnail != null)
            {
                OriginalXboxrom.AddImage(SaveImageToPath(results.Thumbnail, results.TitleID));
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
    public static string SaveImageToPath(Image thumbnail, string titleid)
    {
        var xbox360cache = RomManagerConfiguration.GetOriginalXboxThumbnailCachePath();
        var titleidfolder = Path.Combine(xbox360cache, titleid);
        Directory.CreateDirectory(titleidfolder);
        var imagefilepath = Path.Combine(titleidfolder, "Thumbnail.jpg");
        thumbnail.Save(imagefilepath);
        return imagefilepath;
    }
}


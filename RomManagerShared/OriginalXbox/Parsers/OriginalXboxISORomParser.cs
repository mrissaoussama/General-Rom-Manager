using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.OriginalXbox.Configuration;
using RomManagerShared.Utils;
using RomManagerShared.Utils.ISO2GOD;
using RomManagerShared.Xbox360.Configuration;
using System.ComponentModel;
using System.Drawing;
namespace RomManagerShared.OriginalXbox.Parsers;

public class OriginalXboxISORomParser : IRomParser<OriginalXboxConsole>
{
    public OriginalXboxISORomParser()
    {
        Extensions = ["iso"];
    }
    public List<string> Extensions { get; set; }
    public Task<List<Rom>> ProcessFile(string path)
    {
        List<Rom> list = [];
        IsoDetailsResults? results = null;
        try
        {
            IsoDetailsArgs args = new(path, Path.GetDirectoryName(path) + "\\", Xbox360Configuration.GetXexToolPath());
            DoWorkEventArgs workargs = new(args);
            IsoDetails isoDetails = new();
            results = isoDetails.IsoDetails_DoWork(workargs);
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
        var xbox360cache = OriginalXboxConfiguration.GetThumbnailCachePath();
        var titleidfolder = Path.Combine(xbox360cache, titleid);
        Directory.CreateDirectory(titleidfolder);
        var imagefilepath = Path.Combine(titleidfolder, "Thumbnail.jpg");
        thumbnail.Save(imagefilepath);
        return imagefilepath;
    }
}


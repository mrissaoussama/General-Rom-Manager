using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
using RomManagerShared.Utils.ISO2GOD;
using RomManagerShared.Xbox360.Configuration;
using System.Drawing;
namespace RomManagerShared.Xbox360.Parsers;

public class Xbox360XEXRomParser : IRomParser<Xbox360Console>
{
    public Xbox360XEXRomParser()
    {
        Extensions = ["xex"];
    }
    public List<string> Extensions { get; set; }
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
            Xbox360rom.AddTitleName(results.Name.RemoveTrailingNullTerminators());
            Xbox360rom.TitleID = results.TitleID;
            if (results.Thumbnail != null)
            {
                Xbox360rom.AddImage(SaveImageToPath(results.Thumbnail, results.TitleID));
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
    public static string SaveImageToPath(Image thumbnail, string titleid)
    {
        var xbox360cache = Xbox360Configuration.GetThumbnailCachePath();
        var titleidfolder = Path.Combine(xbox360cache, titleid);
        Directory.CreateDirectory(titleidfolder);
        var imagefilepath = Path.Combine(titleidfolder, "Thumbnail.jpg");
        thumbnail.Save(imagefilepath);
        return imagefilepath;
    }
}


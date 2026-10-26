using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
using RomManagerShared.Utils.ISO2GOD;
using RomManagerShared.Xbox360.Configuration;
using System.ComponentModel;
namespace RomManagerShared.Xbox360.Parsers;

public class Xbox360ISORomParser : IRomParser<Xbox360Console>
{
    public Xbox360ISORomParser()
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
            IsoDetailsArgs args = new(path, FileUtils.GetTempDirectory(), Xbox360Configuration.GetXexToolPath());
            DoWorkEventArgs workargs = new(args);

            IsoDetails isoDetails = new();
            results = isoDetails.IsoDetails_DoWork(workargs);
            if (results is null)
                return Task.FromResult(list);
            Xbox360Game Xbox360rom = new();
            Xbox360rom.AddTitleName(results.Name.RemoveTrailingNullTerminators());
            Xbox360rom.TitleID = results.TitleID;
            if (results.RawThumbnail != null)
            {
                Xbox360rom.AddImage(SaveImageToPath(results.RawThumbnail, results.TitleID));
            }
            Xbox360rom.Size = FileUtils.GetFileSize(path);
            Xbox360rom.Path = path;
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


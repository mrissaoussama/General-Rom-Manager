using Param_SFO;
using PS4_Tools;
using RomManagerShared.Base;
using RomManagerShared.Utils;
namespace RomManagerShared.PS4.Parsers
{
    public class PS4PKGParser : IRomParser
    {
        public PS4PKGParser()
        {
            Extensions = ["pkg"];
        }
        public HashSet<string> Extensions { get; set; }
        public Task<HashSet<Rom>> ProcessFile(string path)
        {
            PS4_Tools.PKG.SceneRelated.Unprotected_PKG ps4Pkg;
            try
            {
                PARAM_SFO f = new();                ps4Pkg = PKG.SceneRelated.Read_PKG(path);
            }
            catch (Exception)
            {
                FileUtils.Log("pkg not valid " + path);
                return Task.FromResult(Array.Empty<Rom>().ToHashSet()); throw;
            }
            Rom ps4rom;
            if (ps4Pkg.PKG_Type == PS4_Tools.PKG.SceneRelated.PKGType.Patch)
            {
                ps4rom = new PS4Update();
            }            else if (ps4Pkg.PKG_Type == PS4_Tools.PKG.SceneRelated.PKGType.Addon)
            {
                ps4rom = new PS4DLC();
            }
            else
            {
                ps4rom = new PS4Game();
            }
            ps4rom.ProductCode = ps4Pkg.Content_ID;
            ps4rom.TitleID = ps4Pkg.Param.TITLEID;
            ps4rom.Version = ps4Pkg.Param.APP_VER;
            //ps4rom.Region = ps4Pkg.Region;
            Param_SFO.PARAM_SFO.Table t = ps4Pkg.Param.Tables.ToList().Where(x => x.Name == "SYSTEM_VER").FirstOrDefault();
            if (t.Name is not null)
            {
                long value = Convert.ToInt64(t.Value);
                ps4rom.MinimumFirmware = PS4Utils.SystemFirmwareLongToString(value);
            }
            if (ps4Pkg.Icon != null)
            {
                ps4rom.Icon = BinUtils.ByteArrayToPrefixedString(ps4Pkg.Icon);
            }            if (ps4Pkg.Image != null)
            {
                ps4rom.Thumbnail = BinUtils.ByteArrayToPrefixedString(ps4Pkg.Image);
            }            if (ps4Pkg.Image2 != null)
            {
                ps4rom.AddImage(BinUtils.ByteArrayToPrefixedString(ps4Pkg.Image2));
            }            ps4rom.Path = path;
            ps4rom.AddTitleName(ps4Pkg.PS4_Title);
            ps4rom.Size = FileUtils.GetFileSize(path); // YourSize;
            ps4rom.IsDemo = false; // YourIsDemo;
            ps4rom.NumberOfPlayers = 0; // YourNumberOfPlayers;
            ps4rom.ReleaseDate = new DateOnly(); // YourReleaseDate;
                                                 //  ps4rom.Hash = new byte[0]; // YourHash;
            HashSet<Rom> list = [ps4rom];            return Task.FromResult(list);
        }
    }
}

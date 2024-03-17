using RomManagerShared.Utils;using System.Text;
namespace RomManagerShared;
//unused
public class _3DSReader
{
    public int ProductCodeOffset { get; set; }
    public int ProductCodeOffset2 { get; set; }
    public _3DSReader()
    {
        ProductCodeOffset = 0x3a90;
        ProductCodeOffset2 = 0x3a50;
    }
    public string GetProductCode(string filepath)
    {
        BinaryReader reader = new(new FileStream(filepath, FileMode.Open));
        reader.BaseStream.Position = ProductCodeOffset;
        var code = reader.ReadBytes(16);
        string utfString = Encoding.UTF8.GetString(code, 0, code.Length);
        return utfString;
    }
    public List<string> GetProductCodesInDirectory(string directory, List<string> extention)
    {
        List<string> codes = [];
        var filepaths = FileUtils.GetFilesInDirectoryWithExtensions(directory, extention);
        foreach (var filepath in filepaths)
        {
            BinaryReader reader = new(new FileStream(filepath, FileMode.Open));
            reader.BaseStream.Position = ProductCodeOffset;
            var code = reader.ReadBytes(16);
            string utfString = Encoding.UTF8.GetString(code, 0, code.Length);
            if (!utfString.Contains("CTR-"))
            {
                codes.Add(filepath);
                //  throw new Exception("Product code not found");
            }
            else codes.Add(utfString);
        }
        return codes;
    }
}

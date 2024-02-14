using RomManagerShared.Base;
using System.Text;

namespace RomManagerShared.WiiU.Parsers
{
    public class WiiUWudWuxParser : IRomParser
    {
        public HashSet<string> Extensions { get; set; }

        public WiiUWudWuxParser()
        {
            Extensions = ["wud", "wux"];
        }

        public async Task<HashSet<Rom>> ProcessFile(string switchRomPath)
        {
            HashSet<Rom> list = [];
            string extension = Path.GetExtension(switchRomPath)?.TrimStart('.').ToLower();

            if (!Extensions.Contains(extension))
            {
                return list;
            }

            string productCode;
            if (extension == "wud")
            {
                productCode = await GetProductCodeFromOffset(switchRomPath, 0x0);
            }
            else 
            {
                productCode = await GetProductCodeFromOffset(switchRomPath, 0x2F0000);
            }

            if (!productCode.StartsWith("WUP"))
            {
                return list;
            }

            WiiUGame wiiUGame = new()
            {
                ProductCode = productCode
            };
            list.Add(wiiUGame);
            return list;
        }

        private static async Task<string> GetProductCodeFromOffset(string filePath, long offset)
        {
            using FileStream fileStream = File.OpenRead(filePath);
            fileStream.Seek(offset, SeekOrigin.Begin);
            byte[] buffer = new byte[22];
            await fileStream.ReadAsync(buffer);
            return Encoding.ASCII.GetString(buffer);
        }
    }
}

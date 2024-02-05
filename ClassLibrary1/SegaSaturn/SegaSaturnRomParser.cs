using RomManagerShared.Base;
using RomManagerShared.Nintendo64.Z64Utils;
using RomManagerShared.SegaSaturn;
using RomManagerShared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RomManagerShared.SegaSaturn.Parsers
{
    public class SegaSaturnRomParser : IRomParser
    {
        public SegaSaturnRomParser()
        {
            Extensions = ["mdf"];
        }
        public HashSet<string> Extensions { get; set; }
        public Task<HashSet<Rom>> ProcessFile(string path)
        {
            SegaSaturnGame segaSaturnrom = new();
            using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
            {
                // Read ID at offset 0x30
                reader.BaseStream.Seek(0x30, SeekOrigin.Begin);
                string id = Encoding.ASCII.GetString(reader.ReadBytes(10));

                // Read version string at offset 0x3A
                reader.BaseStream.Seek(0x3A, SeekOrigin.Begin);
                string version = Encoding.ASCII.GetString(reader.ReadBytes(6));

                // Read date string at offset 0x40
                reader.BaseStream.Seek(0x40, SeekOrigin.Begin);
                string date = Encoding.ASCII.GetString(reader.ReadBytes(8));

                // Read discnumber string at offset 0x48 to 0x4D
                reader.BaseStream.Seek(0x48, SeekOrigin.Begin);
                string discNumber = Encoding.ASCII.GetString(reader.ReadBytes(6));

                // Read region string at offset 0x50
                reader.BaseStream.Seek(0x50, SeekOrigin.Begin);
                string region = Encoding.ASCII.GetString(reader.ReadBytes(3));

                // Read periphcode at offset 0x60 to 0x6F
                reader.BaseStream.Seek(0x60, SeekOrigin.Begin);
                string periphCode = Encoding.ASCII.GetString(reader.ReadBytes(16));

                // Read game name string at offset 0x70 to 0xD0
                reader.BaseStream.Seek(0x70, SeekOrigin.Begin);
                string gameName = Encoding.ASCII.GetString(reader.ReadBytes(160));

                // Remove trailing spaces from strings
                id = id.RemoveTrailingNullTerminators().TrimEnd();
                version = version.RemoveTrailingNullTerminators().TrimEnd();
                date = date.RemoveTrailingNullTerminators().TrimEnd();
                discNumber = discNumber.RemoveTrailingNullTerminators().TrimEnd();
                region = region.RemoveTrailingNullTerminators().TrimEnd();
                periphCode = periphCode.RemoveTrailingNullTerminators().TrimEnd();
                gameName = gameName.RemoveTrailingNullTerminators().TrimEnd();
                DateOnly? gamedate = null;
                segaSaturnrom.TitleID = id;
                segaSaturnrom.Version = version;
                if (DateOnly.TryParseExact(date, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateOnly parsedDateonly))
                {
                    gamedate = parsedDateonly;
                }
                segaSaturnrom.ReleaseDate = gamedate;
                segaSaturnrom.AddRegion(GetRegion(region));
                segaSaturnrom.AddTitleName(gameName);
            }

            HashSet<Rom> list = [segaSaturnrom];
            return Task.FromResult(list);
        }

        private Region GetRegion(string region)
        {
            return region switch
            {
                "U" => Region.USA,
                "JT" => Region.Japan,
                "J" => Region.Japan,
                "JE" => Region.Europe,
                _ => Region.Unknown,
            } ;
        }
    }
}

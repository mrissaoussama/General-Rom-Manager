using RomManagerShared.Base;
using RomManagerShared.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DiscUtils.Iso9660;

namespace RomManagerShared.PSP.Parsers
{
    public class PSPRomParser : IRomParser
    {
        public PSPRomParser()
        {
            Extensions = new HashSet<string> { "iso" };
        }

        public HashSet<string> Extensions { get; set; }

        public Task<List<Rom>> ProcessFile(string path)
        {
            PSPGame pspRom = new PSPGame();

            try
            {
                using (FileStream isoStream = File.Open(path, FileMode.Open, FileAccess.Read))
                {
                    CDReader cd = new CDReader(isoStream, true);

                    // Open PARAM.SFO file
                    Stream fileStream = cd.OpenFile(@"PSP_GAME\PARAM.SFO", FileMode.Open);

                    // Read category (2 bytes from 0x124)
                    fileStream.Seek(0x124, SeekOrigin.Begin);
                    byte[] categoryBytes = new byte[2];
                    fileStream.Read(categoryBytes, 0, categoryBytes.Length);
                    pspRom.Type = Encoding.UTF8.GetString(categoryBytes);

                    // Read title ID (9 bytes from 0x128)
                    fileStream.Seek(0x128, SeekOrigin.Begin);
                    byte[] titleIdBytes = new byte[9];
                    fileStream.Read(titleIdBytes, 0, titleIdBytes.Length);
                    pspRom.TitleID = Encoding.UTF8.GetString(titleIdBytes);

                    // Read disk number (1 byte from 0x138)
                    fileStream.Seek(0x138, SeekOrigin.Begin);
                    pspRom.DiskNumber = fileStream.ReadByte();

                    // Read disk total (1 byte from 0x13C)
                    fileStream.Seek(0x13C, SeekOrigin.Begin);
                    pspRom.DiskTotal = fileStream.ReadByte();

                    // Read version (4 bytes from 0x140)
                    fileStream.Seek(0x140, SeekOrigin.Begin);
                    byte[] versionBytes = new byte[4];
                    fileStream.Read(versionBytes, 0, versionBytes.Length);
                    pspRom.Version = Encoding.UTF8.GetString(versionBytes);

                    // Read parental level (1 byte from 0x148)
                    fileStream.Seek(0x148, SeekOrigin.Begin);
                    pspRom.ParentalLevel = fileStream.ReadByte();

                    // Read minimum PSP system (4 bytes from 0x14C)
                    fileStream.Seek(0x14C, SeekOrigin.Begin);
                    byte[] minPspSystemBytes = new byte[4];
                    fileStream.Read(minPspSystemBytes, 0, minPspSystemBytes.Length);
                    pspRom.MinimumFirmware = Encoding.UTF8.GetString(minPspSystemBytes);

                    // Read title name (from 158 to 1D7 )
                    fileStream.Seek(0x158, SeekOrigin.Begin);
                    List<byte> titleNameBytes = new List<byte>();
                    byte currentByte;
                    while ((currentByte = (byte)fileStream.ReadByte()) != 0x00)
                    {
                        titleNameBytes.Add(currentByte);
                    }
                    pspRom.TitleName = Encoding.UTF8.GetString(titleNameBytes.ToArray());
                    SetRegion(pspRom);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            List<Rom> romList = new List<Rom> { pspRom };
            return Task.FromResult(romList);
        }

        private void SetRegion(PSPGame pspRom)
        {

            switch (pspRom.TitleID[2])
            {
                case 'U':
                    pspRom.Region = "USA";
                    break;
                case 'E':
                    pspRom.Region = "Europe";
                    break;
                case 'J':
                    pspRom.Region = "Japan";
                    break;
                case 'A':
                    pspRom.Region = "Asia";
                    break;
                case 'H':
                    pspRom.Region = "Hong Kong";
                    break;
                case 'K':
                    pspRom.Region = "Korea";
                    break;
                default:
                    pspRom.Region = "Unknown";
                    break;
            }
        }
    }
}

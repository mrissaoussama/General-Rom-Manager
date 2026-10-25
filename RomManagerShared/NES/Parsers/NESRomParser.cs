using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;

namespace RomManagerShared.NES.Parsers;

/// <summary>iNES / NES 2.0 header. NES carts carry no title, so names come from DATs (NESDatProvider).</summary>
public class NESRomParser : IRomParser<NESConsole>
{
    public List<string> Extensions { get; set; } = ["nes"];

    public Task<List<Rom>> ProcessFile(string path)
    {
        try
        {
            var header = new byte[16];
            using (var fs = FileUtils.OpenRead(path))
                if (fs.Read(header, 0, 16) < 16) return Task.FromResult<List<Rom>>([]);

            var game = new NESGame { Path = path, Size = new FileInfo(path).Length };
            game.AddTitleName(Path.GetFileNameWithoutExtension(path));
            if (header[0] == 'N' && header[1] == 'E' && header[2] == 'S' && header[3] == 0x1A)
            {
                var nes2 = (header[7] & 0x0C) == 0x08;
                game.HeaderFormat = nes2 ? "NES 2.0" : "iNES";
                game.Mapper = (header[6] >> 4) | (header[7] & 0xF0) | (nes2 ? (header[8] & 0x0F) << 8 : 0);
                var prg = header[4] | (nes2 ? (header[9] & 0x0F) << 8 : 0);
                var chr = header[5] | (nes2 ? (header[9] & 0xF0) << 4 : 0);
                game.PrgRom = $"{prg * 16} KB";
                game.ChrRom = chr == 0 ? "CHR RAM" : $"{chr * 8} KB";
            }
            else
            {
                game.HeaderFormat = "None";
            }
            return Task.FromResult<List<Rom>>([game]);
        }
        catch (Exception ex)
        {
            FileUtils.Log($"[NES] {path}: {ex.Message}");
            return Task.FromResult<List<Rom>>([]);
        }
    }
}

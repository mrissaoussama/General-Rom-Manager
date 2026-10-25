using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;

namespace RomManagerShared.MasterSystem.Parsers;

/// <summary>Reads the TMR SEGA header; titles come from the file name or DATs (MasterSystemDatProvider).</summary>
public class MasterSystemRomParser : IRomParser<MasterSystemConsole>
{
    public List<string> Extensions { get; set; } = ["sms"];

    public Task<List<Rom>> ProcessFile(string path)
    {
        try
        {
            var game = new MasterSystemGame { Path = path, Size = new FileInfo(path).Length };
            game.AddTitleName(Path.GetFileNameWithoutExtension(path));
            game.Apply(SegaTmrHeader.Read(path));
            return Task.FromResult<List<Rom>>([game]);
        }
        catch (Exception ex)
        {
            FileUtils.Log($"[MasterSystem] {path}: {ex.Message}");
            return Task.FromResult<List<Rom>>([]);
        }
    }
}

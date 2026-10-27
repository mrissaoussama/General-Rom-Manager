using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;

namespace RomManagerShared.GameGear.Parsers;

/// <summary>Reads the TMR SEGA header; titles come from the file name or DATs (GameGearDatProvider).</summary>
public class GameGearRomParser : IRomParser<GameGearConsole>
{
    public List<string> Extensions { get; set; } = ["gg"];

    public Task<List<Rom>> ProcessFile(string path)
    {
        try
        {
            var game = new GameGearGame { Path = path, Size = new FileInfo(path).Length };
            game.AddTitleName(Path.GetFileNameWithoutExtension(path));
            game.Apply(SegaTmrHeader.Read(path));
            return Task.FromResult<List<Rom>>([game]);
        }
        catch (Exception ex)
        {
            AppLog.Warn($"[GameGear] {path}: {ex.Message}");
            return Task.FromResult<List<Rom>>([]);
        }
    }
}

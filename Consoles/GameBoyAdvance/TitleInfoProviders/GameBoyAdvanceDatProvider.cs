using RomManagerShared.Base;
using RomManagerShared.GameBoyAdvance;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils.Dat;
using RomManagerShared.Utils;

namespace RomManagerShared.GameBoyAdvance.TitleInfoProviders;

public class GameBoyAdvanceDatProvider : DatTitleInfoProvider<GameBoyAdvanceConsole>
{
    protected override bool MatchesDat(string f) => f.Contains("Game Boy Advance", StringComparison.OrdinalIgnoreCase);

    protected override string? LibretroSystem(Rom rom) => "Nintendo - Game Boy Advance";
}

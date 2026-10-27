using RomManagerShared.Base;
using RomManagerShared.GameBoy;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils.Dat;
using RomManagerShared.Utils;

namespace RomManagerShared.GameBoy.TitleInfoProviders;

public class GameBoyDatProvider : DatTitleInfoProvider<GameBoyConsole>
{
    protected override string? LibretroSystem(Rom rom) =>
        rom.Path?.EndsWith(".gbc", StringComparison.OrdinalIgnoreCase) == true ? "Nintendo - Game Boy Color" : "Nintendo - Game Boy";

    // Covers "Game Boy" and "Game Boy Color", not "Game Boy Advance".
    protected override bool MatchesDat(string f) =>
        f.Contains("Nintendo - Game Boy", StringComparison.OrdinalIgnoreCase) && !f.Contains("Advance", StringComparison.OrdinalIgnoreCase);
}

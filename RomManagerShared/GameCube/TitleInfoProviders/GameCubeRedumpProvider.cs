using RomManagerShared.Base;
using RomManagerShared.Configuration;
using RomManagerShared.GameCube;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils.Dat;
using RomManagerShared.Utils;
using System.Buffers;
using System.Security.Cryptography;

namespace RomManagerShared.GameCube.TitleInfoProviders;

public class GameCubeRedumpProvider : DiscDatTitleInfoProvider<GameCubeConsole>
{
    protected override bool MatchesDat(string f) => f.Contains("Nintendo - GameCube", StringComparison.OrdinalIgnoreCase);
}

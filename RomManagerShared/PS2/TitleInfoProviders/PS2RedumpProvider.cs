using RomManagerShared.Base;
using RomManagerShared.Configuration;
using RomManagerShared.Interfaces;
using RomManagerShared.PS2;
using RomManagerShared.Utils.Dat;
using RomManagerShared.Utils;
using System.Buffers;
using System.Security.Cryptography;

namespace RomManagerShared.PS2.TitleInfoProviders;

public class PS2RedumpProvider : DiscDatTitleInfoProvider<PS2Console>
{
    protected override bool MatchesDat(string f) => f.Contains("Sony - PlayStation 2", StringComparison.OrdinalIgnoreCase);
}

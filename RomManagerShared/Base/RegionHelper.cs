
namespace RomManagerShared.Base;

public class RegionHelper
{
    public static Region ParseRegion(string r)
    {
        return r switch
        {
            "JP" => Region.Japan,
            _ => Region.Unknown,
        };
    }
}
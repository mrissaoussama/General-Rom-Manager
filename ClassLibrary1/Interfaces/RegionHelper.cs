
namespace RomManagerShared.Base
{
    public class RegionHelper
    {
        public static Region ParseRegion(string r)
        {
            switch (r)
            {
                case "JP":
                    return Region.Japan;
                default: return Region.Unknown;
            }
        }
    }
}
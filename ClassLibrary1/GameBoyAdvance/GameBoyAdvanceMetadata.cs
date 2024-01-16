using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RomManagerShared.GameBoyAdvance
{
    public class GameBoyAdvanceMetadata
    {

        public string Title { get; set; }
        public string GameCode { get; set; }
        public string MakerCode { get; set; }
        public string UnitCode { get; set; }
        public string VersionCode { get; set; }
        public string HeaderChecksum { get; set; }

        public string GetGameTypeChar()
        {
            if (GameCode.Length == 4)
            {
                return GameCode[0].ToString();
            }
            return "";
        }

        public string GetCountryChar()
        {
            if (GameCode.Length == 4)
            {
                return GameCode.Substring(3);
            }
            return "";
        }
    }
}

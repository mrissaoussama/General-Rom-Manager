using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RomManagerShared.Base;

namespace RomManagerShared.ThreeDS
{
    public interface IThreeDSRom { }
    public class ThreeDSGame:Game, IThreeDSRom
    {
        public ThreeDSGame() : base()
        {
        }
    }
    public class ThreeDSDLC : DLC, IThreeDSRom
    {
        public ThreeDSDLC() : base()
        {
        }
    }
    public class ThreeDSUpdate : Update, IThreeDSRom
    {
        public ThreeDSUpdate() : base()
        {
        }
    }
}

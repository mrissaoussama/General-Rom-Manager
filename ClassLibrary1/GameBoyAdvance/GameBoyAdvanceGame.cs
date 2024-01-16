using RomManagerShared.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RomManagerShared.GameBoyAdvance
{
    public interface IGameBoyAdvanceRom { }
    public class GameBoyAdvanceGame:Game, IGameBoyAdvanceRom
    {
        public GameBoyAdvanceGame() : base()
        {
        }
    }

}

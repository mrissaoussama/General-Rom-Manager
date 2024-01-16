using RomManagerShared.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RomManagerShared.GameBoy
{
    public interface IGameBoyRom { }
    public class GameBoyGame:Game, IGameBoyRom
    {
        public GameBoyGame() : base()
        {
        }
    }

}

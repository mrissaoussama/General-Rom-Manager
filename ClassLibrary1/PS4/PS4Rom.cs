using RomManagerShared.Base;
namespace RomManagerShared.PS4
{
    public interface IPS4Rom { }
    public class PS4Game : Game, IPS4Rom
    {
        public PS4Game() : base()
        {
        }
    }
    public class PS4Update : Update, IPS4Rom
    {
        public PS4Update() : base()
        {
        }
    }
    public class PS4DLC : DLC, IPS4Rom
    {
        public PS4DLC() : base()
        {
        }
    }
}

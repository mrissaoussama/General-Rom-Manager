﻿using RomManagerShared.Base;
{
    public interface IPSVitaRom { }
    public class PSVitaGame : Game, IPSVitaRom
    {
        public PSVitaGame() : base()
        {
        }
    }
    public class PSVitaDLC : DLC, IPSVitaRom
    {
        public PSVitaDLC() : base()
        {
        }
    public class PSVitaUpdate : Update, IPSVitaRom
    {
        public PSVitaUpdate() : base()
        {
        }
    }
}
﻿using RomManagerShared.Base;

public interface IWiiRom { }
public class WiiGame : Game, IWiiRom
{
    public WiiGame() : base()
    {
    }
}
public class WiiWadGame : Game, IWiiRom
{
    public WiiWadGame() : base()
    {
    }
}
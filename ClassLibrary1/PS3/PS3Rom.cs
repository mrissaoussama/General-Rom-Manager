﻿using RomManagerShared.Base;

public interface IPS3Rom { }
public class PS3Game : Game, IPS3Rom
{
    public PS3Game() : base()
    {
    }
}
public class PS3DLC : DLC, IPS3Rom
{
    public PS3DLC() : base()
    {
    }
public class PS3Update : Update, IPS3Rom
{
    public PS3Update() : base()
    {
    }
}
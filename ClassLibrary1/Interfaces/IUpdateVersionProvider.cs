﻿namespace RomManagerShared.Interfaces;

public interface IUpdateVersionProvider
{
    public string Source { get; set; }
    Task LoadVersionDatabaseAsync();
}
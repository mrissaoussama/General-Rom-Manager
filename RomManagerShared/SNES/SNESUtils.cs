﻿namespace RomManagerShared.SNES;

public static class SNESUtils
{
    private static readonly string[] Extensions = ["sfc"];
    {
        string fileExtension = System.IO.Path.GetExtension(filePath).TrimStart('.');
        return Extensions.Contains(fileExtension);

    }
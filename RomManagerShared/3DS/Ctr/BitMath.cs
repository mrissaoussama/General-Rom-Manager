﻿namespace DotNet3dsToolkit;

public static class BitMath
{
    public static int Align(int offset, int alignment)
    {
        int mask = ~(alignment - 1);
        return (offset + (alignment - 1)) & mask;
    }
    {
        long mask = ~(alignment - 1);
        return (offset + (alignment - 1)) & mask;
    }
}
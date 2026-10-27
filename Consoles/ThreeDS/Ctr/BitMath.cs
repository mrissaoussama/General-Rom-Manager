// Vendored code: nullable warnings are not fixed here.
#nullable disable warnings
namespace DotNet3dsToolkit;

public static class BitMath
{
    public static int Align(int offset, int alignment)
    {
        int mask = ~(alignment - 1);
        return (offset + (alignment - 1)) & mask;
    }
    public static long Align(long offset, int alignment)
    {
        long mask = ~(alignment - 1);
        return (offset + (alignment - 1)) & mask;
    }
}

namespace RomManagerShared.Utils;

public static class StringUtils
{
    public static string RemoveTrailingNullTerminators(this string s)
    {
        int nullIndex = s.IndexOf('\0');
        return nullIndex != -1 ? s[..nullIndex] : s;
    }
    //
    //public static bool IsNullOrEmpty(this string? s)
    //{
    //    return string.IsNullOrEmpty(s);
    //}
}
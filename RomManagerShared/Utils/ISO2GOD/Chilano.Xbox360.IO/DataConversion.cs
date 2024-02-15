using System.Text;

namespace RomManagerShared.Utils.ISO2GOD.Chilano.Xbox360.IO;

public static class DataConversion
{
    public static string BytesToHexString(byte[] value)
    {
        StringBuilder stringBuilder = new(value.Length * 2);
        foreach (byte b in value)
        {
            stringBuilder.Append(b.ToString("X02"));
        }
        return stringBuilder.ToString();
    }
}

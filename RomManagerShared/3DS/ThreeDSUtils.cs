namespace RomManagerShared.ThreeDS;

public class ThreeDSUtils
{
    public enum TidCategory
    {
        Normal = 0x0,
        DlpChild = 0x1,
        Demo = 0x2,
        AddOnContents = 0x4,
        NoExecute = 0x8,
        Update = 0xE,
        System = 0x10,
        NotMount = 0x80,
        Dlc = 0x8C,
        Twl = 0x8000
    }
    public static TidCategory DetectContentCategory(string titleId)
    {
        int consolePlatform = int.Parse(titleId[..1]);
        int contentCategory = GetContentCategory(titleId);
        int uniqueId = int.Parse(titleId.Substring(2, 6), System.Globalization.NumberStyles.HexNumber);
        int titleIdVariation = int.Parse(titleId.Substring(8, 2), System.Globalization.NumberStyles.HexNumber);        TidCategory category = (TidCategory)contentCategory;
        return category;
    }    static int GetContentCategory(string titleId)
    {
        int abcd = int.Parse(titleId.Substring(4, 4), System.Globalization.NumberStyles.HexNumber);
        return abcd;
    }    internal static Type? GetRomMetadataClass(string titleID)
    {
        throw new NotImplementedException();
    }
}

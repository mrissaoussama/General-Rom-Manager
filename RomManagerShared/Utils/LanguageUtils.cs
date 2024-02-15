namespace RomManagerShared.Utils;

public enum Language
{
    Unknown,
    English,
    Japanese,
    French,
    Spanish,
    German, Italian,
    Chinese,
    Korean,
    Swedish,
    Danish,
    Norwegian,
    Russian,
    Dutch,
    Portuguese,
    TraditionalChinese,
    SimplifiedChinese,
}
public static class LanguageUtils
{
    public static Language ConvertToLanguage(string languageCode)
    {
        return languageCode.ToLower() switch
        {
            "en" => Language.English,
            "ja" => Language.Japanese,
            "fr" => Language.French,
            "es" => Language.Spanish,
            "de" => Language.German,
            "it" => Language.Italian,
            "zh" => Language.Chinese,
            "ko" => Language.Korean,
            "sv" => Language.Swedish,
            "da" => Language.Danish,
            "no" => Language.Norwegian,
            "ru" => Language.Russian,
            _ => Language.Unknown,
        };
    }
}
namespace RomManagerShared.Utils
{
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
    }
    public static class LanguageUtils
    {
        public static Language ConvertToLanguage(string languageCode)
        {
            switch (languageCode.ToLower())
            {
                case "en":
                    return Language.English;
                case "ja":
                    return Language.Japanese;
                case "fr":
                    return Language.French;
                case "es":
                    return Language.Spanish;
                case "de":
                    return Language.German;
                case "it":
                    return Language.Italian;
                case "zh":
                    return Language.Chinese;
                case "ko":
                    return Language.Korean;
                case "sv":
                    return Language.Swedish;
                case "da":
                    return Language.Danish;
                case "no":
                    return Language.Norwegian;
                case "ru":
                    return Language.Russian;
                default:
                    return Language.Unknown;
            }
        }
    }
}
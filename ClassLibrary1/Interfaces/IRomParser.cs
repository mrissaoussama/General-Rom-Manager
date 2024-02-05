namespace RomManagerShared.Base
{
    public interface IRomParser
    {
        HashSet<string> Extensions { get; set; }
        Task<HashSet<Rom>> ProcessFile(string path);    }
}

namespace RomManagerShared.Base
{
    public interface IRomParser
    {
        HashSet<string> Extensions { get; set; }
        Task<HashSet<Rom>> ProcessFile(string path);    }
    //public interface IRomParser<T> where T : Rom
    //{
    //    HashSet<string> Extensions { get; set; }
    //    Task<HashSet<T>> ProcessFile(string path);
    //}

}

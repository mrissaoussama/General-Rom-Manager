using RomManagerShared.Interfaces;

namespace RomManagerShared.Base;

public interface IRomParser<T>   where T : GamingConsole
{
    List<string> Extensions { get; set; }
    Task<List<Rom>> ProcessFile(string path);}
//public abstract class RomParser<T> : IRomParser<T> where T : GamingConsole
//{
//    public List<string> Extensions { get; set; } = [];
//   public async Task<List<Rom>> ProcessFile(string path)
//    {
//        return [];
//    }
//}
//public interface IRomParser<T> where T : Rom
//{
//    List<string> Extensions { get; set; }
//    Task<List<T>> ProcessFile(string path);
//}

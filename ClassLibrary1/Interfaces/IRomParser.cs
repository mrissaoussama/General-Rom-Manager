using System.Threading.Tasks;

namespace RomManagerShared.Base
{
    public interface IRomParser
    {
        HashSet<string> Extensions { get; set; }
        Task<List<Rom>> ProcessFile(string path);

    }
}

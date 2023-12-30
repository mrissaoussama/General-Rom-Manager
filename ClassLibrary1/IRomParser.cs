using RomManagerShared;
using System.Threading.Tasks;

namespace RomManagerShared
{
    public interface IRomParser
    {
        Task<List<IRom>> ProcessFile(string path);
    }
}

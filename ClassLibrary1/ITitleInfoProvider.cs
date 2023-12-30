using RomManagerShared;
using System.Text.Json;

namespace RomManagerShared
{
    public interface ITitleInfoProvider
    {public string Source { get; set; }
        public Task LoadTitleDatabaseAsync();
        Task<IRom> GetTitleInfo(IRom rom);
    }

}

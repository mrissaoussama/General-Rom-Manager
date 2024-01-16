using RomManagerShared.Base;

namespace RomManagerShared.Interfaces
{
    public interface IConsoleManager
    {
        RomParserExecutor RomParserExecutor { get; set; }
        List<Rom> RomList { get; set; }
        Task ProcessFile(string file);
        Task Setup();
    }
}
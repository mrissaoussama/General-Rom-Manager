using RomManagerShared.Base;namespace RomManagerShared.Interfaces;

public interface IConsoleManager
{
    RomParserExecutor RomParserExecutor { get; set; }
    HashSet<Rom> RomList { get; set; }
    Task ProcessFile(string file);
    Task Setup();
}
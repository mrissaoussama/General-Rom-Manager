namespace RomManagerShared.Switch
{
    public interface IConsoleManager
    {
        RomParserExecutor RomParserExecutor { get; set; }
        List<IRom> RomList { get; set; }
        Task ProcessFile(string file);
        Task Setup();
    }
}
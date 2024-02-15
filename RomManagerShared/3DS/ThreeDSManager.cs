using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.ThreeDS.Configuration;
using RomManagerShared.ThreeDS.TitleInfoProviders;
namespace RomManagerShared.ThreeDS;

public class ThreeDSManager : ConsoleManager<ThreeDSConsole>
{
    private readonly ThreeDSJsonTitleInfoProvider titleInfoProvider;
    public HashSet<Rom> RomList { get; set; }
    public ThreeDSManager()
    {
        RomList = [];
        RomParserExecutor = new RomParserExecutor();
        RomParserExecutor.AddParser(new ThreeDsRomParser());
        var regionspath = ThreeDSConfiguration.GetTitleDBPath();
        titleInfoProvider = new ThreeDSJsonTitleInfoProvider(regionspath);
    }
    public RomParserExecutor RomParserExecutor { get; set; }
    public override async Task ProcessFile(string file)
    {
        var processedhash = await RomParserExecutor.ExecuteParsers(file);
        var processedlist = processedhash.ToList();
        for (int i = 0; i < processedlist.Count; i++)
        {
            if (processedlist[i].TitleID is null)
                Console.WriteLine("index {0} is null, filepath={0}", i, file);
            else
            {
                processedlist[i] = await titleInfoProvider.GetTitleInfo(processedlist[i]);
            }
        }
        RomList.UnionWith(processedlist);
    }
    public override async Task Setup()
    {
        await titleInfoProvider.LoadTitleDatabaseAsync();
    }

   
}

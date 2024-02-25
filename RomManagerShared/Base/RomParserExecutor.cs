using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
namespace RomManagerShared;

public class RomParserExecutor<T> where T : GamingConsole
{
    public List<IRomParser<T>> Parsers = [];

    public RomParserExecutor(IEnumerable<IRomParser<T>> titleparsers)
    {
        Parsers = titleparsers.ToList() ;
    }

    public void AddParser(IRomParser<T> parser)
    {
        Parsers.Add(parser);
        Parsers = [.. Parsers.OrderByDescending(p => p.Extensions.Count)];
    }
    public async Task<List<Rom>> ExecuteParsers(string file)
    {
        List<Rom> mergedRomList = [];
        var CompatibleParsers = Parsers.Where(x => x.Extensions.Contains(Path.GetExtension(file).Replace(".", "").ToLower())).ToList();
        foreach (var parser in CompatibleParsers)

        {
            var parsedRomList = new List<Rom>();
            try
            {
                parsedRomList = await parser.ProcessFile(file);
                if (parsedRomList == null || parsedRomList.Count == 0)
                {
                    continue;
                }
                //f title id exists, merged properties
                for (int i = 0; i < parsedRomList.Count; i++)
                {
                    var parsedrom = parsedRomList[i];
                    foreach (var mergedrom in mergedRomList)
                    {
                        if (parsedrom.TitleID == mergedrom.TitleID)
                        {
                            RomUtils.CopyNonNullProperties(parsedrom, mergedrom);
                            parsedRomList.RemoveAt(i);
                            i--;
                            break; 
                        }
                    }
                }
                mergedRomList.AddRange(parsedRomList);
                
            }
            catch (Exception ex)
            {
                FileUtils.Log($"file '{file}' threw exception {ex.Message}.{Environment.NewLine}");
                continue;
            }
        }
        if (mergedRomList.Count == 0)
        {
            //   FileUtils.MoveFileToErrorFiles(file);
        }
        return mergedRomList;
    }
    public List<string> GetSupportedExtensions()
    {
        if (Parsers.Count == 0)
        {
            return [];
        }
        List<string> extensionList = [];
        foreach (var parser in Parsers)
        {
            extensionList.AddRange(parser.Extensions);
        }
        return extensionList;
    }

}

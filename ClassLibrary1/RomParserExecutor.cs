using RomManagerShared.Base;
using RomManagerShared.Utils;
namespace RomManagerShared
{
    public class RomParserExecutor
    {
        public List<IRomParser> Parsers = [];
        public RomParserExecutor AddParser(IRomParser parser)
        {
            Parsers.Add(parser);
            Parsers = [.. Parsers.OrderByDescending(p => p.Extensions.Count)];
            return this;
        }
        public async Task<HashSet<Rom>> ExecuteParsers(string file)
        {
            HashSet<Rom> mergedRomList = [];
            foreach (var parser in Parsers)

            {
                var parsedRomList = new HashSet<Rom>();
                try
                {
                    parsedRomList = await parser.ProcessFile(file);
                    if (parsedRomList == null || parsedRomList.Count == 0)
                    {
                        continue;
                    }
                    mergedRomList.UnionWith(parsedRomList);
                    break;
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
        public HashSet<string> GetSupportedExtensions()
        {
            if (Parsers.Count == 0)
            {
                return [];
            }
            HashSet<string> extensionhashset = [];
            foreach (var parser in Parsers)
            {
                extensionhashset.UnionWith(parser.Extensions);
            }
            return extensionhashset;
        }
    }
}

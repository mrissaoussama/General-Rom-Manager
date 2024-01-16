using RomManagerShared.Base;
using RomManagerShared.Utils;

namespace RomManagerShared
{
    public class RomParserExecutor
    {
        public  List<IRomParser> Parsers = [];

        public RomParserExecutor AddParser(IRomParser parser)
        {
            Parsers.Add(parser);
            Parsers = [.. Parsers.OrderByDescending(p => p.Extensions.Count)];
            return this;
        }

        public async Task<List<Rom>> ExecuteParsers(string file)
        {
            List<Rom> mergedRomList = [];
            foreach (var parser in Parsers)
            {
                var parsedRomList = new List<Rom>();
                try
                {
                    parsedRomList = await parser.ProcessFile(file);
                }
                catch (Exception ex)
                {
                    FileUtils.Log( $"file '{file}' threw exception {ex.Message}.{Environment.NewLine}");
                }
                mergedRomList.AddRange(parsedRomList);
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

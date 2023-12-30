namespace RomManagerShared
{
    public class RomParserExecutor
    {
        private readonly List<IRomParser> parsers = new List<IRomParser>();

        public RomParserExecutor AddParser(IRomParser parser)
        {
            parsers.Add(parser);
            return this;
        }

        public async Task<List<IRom>> ExecuteParsers(string file)
        {

            List<IRom> mergedRomList = new List<IRom>();
            foreach (var parser in parsers)
            {
                var parsedRomList = new List<IRom>();
                try
                {
                    parsedRomList = await parser.ProcessFile(file);

                }
                catch (Exception ex)
                {
                    string logFilePath = "error.log"; // Replace with your desired log file path

                    FileUtils.Log( $"file '{file}' threw exception {ex.Message}.{Environment.NewLine}");
                    FileUtils.MoveFileToErrorFiles(file); 

                    return new List<IRom>();
                }
                if (parsedRomList is null)
                {
                    Console.WriteLine($"null list on {file}");

                    continue;
                }
                var nulllist = parsedRomList.Where(x => x == null).ToList();
                if (nulllist.Count > 0)

                {
                    Console.WriteLine($"null rom on {file}");
                }
                mergedRomList.AddRange(parsedRomList);

            }

            return mergedRomList;
        }


    }

}

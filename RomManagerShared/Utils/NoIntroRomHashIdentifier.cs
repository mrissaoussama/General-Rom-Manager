using LibHac.Ncm;
using Microsoft.EntityFrameworkCore;
using RomManagerShared.Base;
using RomManagerShared.Base.Database;
using RomManagerShared.Configuration;
using RomManagerShared.Interfaces;
using RomManagerShared.SNES;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace RomManagerShared.Utils
{
    [XmlRoot("datafile")]
    public class NoIntroDataFile
    {
        public int Id { get; set; }

        [XmlElement("header")]
        public virtual NoIntroHeader? Header { get; set; }

        [XmlElement("game")]
        public virtual List<NoIntroGame>? Games { get; set; }
    }

    public class NoIntroHeader
    {
        [XmlElement("id")]
        public int HeaderId { get; set; }

        public int Id { get; set; }
        [XmlElement("name")]

        public string? Name { get; set; }
        [XmlElement("description")]

        public string? Description { get; set; }
        [XmlElement("version")]

        public string? Version { get; set; }

    }

    public class NoIntroGame
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        public int Id { get; set; }

        [XmlAttribute("description")]
        public string? Description { get; set; }

        [XmlAttribute("category")]
        public string? Category { get; set; }
        [XmlElement("rom")]
        public virtual List<NoIntroRom> Roms { get; set; }

        // Additional property
        [XmlElement("game_id")]
        public string? GameId { get; set; }
    }
    public class NoIntroRom

    {
        [XmlAttribute("size")]

        public string? Size { get; set; }
        public int Id { get; set; }

        [XmlAttribute("name")]
        public string? Name { get; set; }

   
        [XmlAttribute("crc")]
        public string? CRC { get; set; }

        [XmlAttribute("md5")]
        public string? MD5 { get; set; }

        [XmlAttribute("sha1")]
        public string? SHA1 { get; set; }

        [XmlAttribute("sha256")]
        public string? SHA256 { get; set; }

        [XmlAttribute("status")]
        public string? Status { get; set; }

        // Additional attributes
        [XmlAttribute("serial")]
        public string? Serial { get; set; }
    }

    public class NoIntroRomHashIdentifier
    {
        public AppDbContext _appDbContext;
        public NoIntroRomHashIdentifier(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }
        public Task<List<Rom>> IdentifyRomByHash(string path)
        {
           
                    return IdentifySNES(path);
         
        }
        public async Task Setup(bool updateDatabase=false)
        {
            bool isDataFilesEmpty = !await _appDbContext.NoIntroDataFiles.AnyAsync();
            if (updateDatabase is false && !isDataFilesEmpty)
                return;
            var nointrodir = RomManagerConfiguration.GetNoIntroPath();
          var filelist=  FileUtils.GetFilesInDirectoryWithExtensions(nointrodir, ["dat"]);
         var nointrolist=   await LoadDataFilesFromPathsAsync(filelist.ToArray(), [], []) ;
            await SaveDataFilesToDbContextAsync(nointrolist);
        }
        public async Task<List<Rom>> IdentifySNES(string path)
        {
            List<RomHash> romHashes =[];

               romHashes = await HashUtils.CalculateFileHashes(path, Enum.GetValues<HashTypeEnum>());
            var nointrogames = await FindDataFilesByHashAsync(romHashes.Select(x=>x.Value).ToArray());
            RomHash rh = new();
            rh.Value = nointrogames.First().Games.First().Roms.First().CRC;
            rh.Type = HashTypeEnum.CRC32;
            List<Rom> roms = new();
            SNESGame game = new();
            game.Hashes = [];
            game.Hashes.Add(rh);
            return roms;
        }
        public async Task<List<NoIntroDataFile>> FindDataFilesByHashAsync(string[] hashes)
        {
            var matchingDataFiles = await _appDbContext.NoIntroDataFiles
                .Include(dataFile => dataFile.Header) // Include the header
                .Select(dataFile => new NoIntroDataFile
                {
                    Id = dataFile.Id,
                    Header = dataFile.Header,
                    Games = dataFile.Games.Where(game =>
                        game.Roms.Any(rom =>
                            (rom.CRC != null && hashes.Any(hash => hash.ToLower() == rom.CRC.ToLower())) ||
                            (rom.MD5 != null && hashes.Any(hash => hash.ToLower() == rom.MD5.ToLower())) ||
                            (rom.SHA1 != null && hashes.Any(hash => hash.ToLower() == rom.SHA1.ToLower())))
                    ).ToList()
                })
                .Where(dataFile => dataFile.Games.Any()) // Filter data files with matching games
                .ToListAsync();

            return matchingDataFiles;
        }
        public async Task SaveDataFilesToDbContextAsync(List<NoIntroDataFile> dataFiles)
        {
            foreach (var dataFile in dataFiles)
            {
                // Check if the data file already exists in the database
                var existingDataFile = await _appDbContext.NoIntroDataFiles
                    .Include(df => df.Games).ThenInclude(g => g.Roms)
                    .FirstOrDefaultAsync(df =>
                        df.Header.HeaderId == dataFile.Header.HeaderId);

                if (existingDataFile == null)
                {
                    // If the data file doesn't exist, add it to the context
                    await _appDbContext.NoIntroDataFiles.AddAsync(dataFile);
                }
                else
                {
                    // If the data file exists, update its games and ROMs
                    foreach (var game in dataFile.Games)
                    {
                        // Check if the game already exists in the data file
                        var existingGame = existingDataFile.Games.FirstOrDefault(g => g.Name == game.Name);

                        if (existingGame == null)
                        {
                            // If the game doesn't exist, add it to the data file
                            existingDataFile.Games.Add(game);
                        }
                        else
                        {
                            // If the game exists, update its ROMs
                            foreach (var rom in game.Roms)
                            {
                                // Check if the ROM already exists in the game
                                var existingRom = existingGame.Roms.FirstOrDefault(r =>
                                    r.Name == rom.Name && r.CRC == rom.CRC && r.MD5 == rom.MD5 && r.SHA1 == rom.SHA1);

                                if (existingRom == null)
                                {
                                    // If the ROM doesn't exist, add it to the game
                                    existingGame.Roms.Add(rom);
                                }
                                else
                                {
                                    // If the ROM exists, update its properties if needed
                                    existingRom.Name = rom.Name;
                                    existingRom.CRC = rom.CRC;
                                    existingRom.MD5 = rom.MD5;
                                    existingRom.SHA1 = rom.SHA1;
                                    existingRom.Size = rom.Size;
                                    existingRom.SHA256 = rom.SHA256;
                                    existingRom.Status = rom.Status;
                                    existingRom.Serial = rom.Serial;
                                }
                            }
                        }
                    }
                }
            }

            await _appDbContext.SaveChangesAsync();
        }

        public async Task<List<NoIntroDataFile>> LoadDataFilesFromPathsAsync(string[] filePaths, string[] includeKeywords, string[] excludeKeywords)
        {
            var filteredFilePaths = FilterFilePaths(filePaths, includeKeywords, excludeKeywords);
            var dataFiles = new List<NoIntroDataFile>();

            foreach (var filePath in filteredFilePaths)
            {
                var dataFile = await ParseFileAsync(filePath);
                if (dataFile != null)
                {
                    dataFiles.Add(dataFile);
                }
            }

            return dataFiles;
        }

        private List<string> FilterFilePaths(string[] filePaths, string[] includeKeywords, string[] excludeKeywords)
        {
            var filteredFilePaths = filePaths.ToList();

            if (includeKeywords != null && includeKeywords.Length > 0)
            {
                filteredFilePaths = filteredFilePaths.Where(filePath => includeKeywords.Any(keyword => filePath.Contains(keyword))).ToList();
            }

            if (excludeKeywords != null && excludeKeywords.Length > 0)
            {
                filteredFilePaths = filteredFilePaths.Where(filePath => !excludeKeywords.Any(keyword => filePath.Contains(keyword))).ToList();
            }

            return filteredFilePaths;
        }

        private async Task<NoIntroDataFile> ParseFileAsync(string filePath)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var settings = new XmlReaderSettings
                    {
                        Async = true,
                        IgnoreWhitespace = true
                    };

                    using (var reader = XmlReader.Create(stream, settings))
                    {
                        var serializer = new XmlSerializer(typeof(NoIntroDataFile));
                        return await Task.Run(() => (NoIntroDataFile)serializer.Deserialize(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing file {filePath}: {ex.Message}");
                return null;
            }
        }
    }
}


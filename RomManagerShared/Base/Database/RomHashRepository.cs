using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RomManagerShared.Base.Database;

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

public class RomHashRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<RomHash> RomHashes;

    public RomHashRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        RomHashes = context.RomHashes;
    }
    public async Task AddIfNewRange(IEnumerable<RomHash> romHashes)
    {
        int addedCount = 0;

        foreach (var romHash in romHashes)
        {
            var existingRomHash = RomHashes.FirstOrDefault(rh =>
                rh.Filename == romHash.Filename &&
                rh.Extension == romHash.Extension &&
                rh.Value == romHash.Value);

            // If no RomHash with the same filename, extension, and value exists, add the new RomHash
            if (existingRomHash == null)
            {
                await RomHashes.AddAsync(romHash);
                addedCount++;
            }
             await _context.SaveChangesAsync();

        }

    }
    public async Task<int> AddIfNew(RomHash romHash)
    {
        // Check if a RomHash with the same filename, extension, and value already exists
        var existingRomHash = RomHashes.FirstOrDefault(rh =>
            rh.Filename == romHash.Filename &&
            rh.Extension == romHash.Extension &&
            rh.Value == romHash.Value);

        // If no RomHash with the same filename, extension, and value exists, add the new RomHash
        if (existingRomHash == null)
        {
            RomHashes.Add(romHash);
            return await _context.SaveChangesAsync();
        }
        return 0;
    }
    // Method to get a RomHash by its ID
    public RomHash GetRomHashById(int id)
    {
        return RomHashes.FirstOrDefault(rh => rh.Id == id);
    }

    // Method to get a collection of RomHashes by Rom ID
    public IEnumerable<RomHash> GetRomHashesByRomId(int romId)
    {
        return _context.Roms.First(rh => rh.Id == romId).Hashes;
    }

    // Method to get a collection of RomHashes by Title ID
    public IEnumerable<RomHash> GetRomHashesByTitleId(string titleId)
    {
        var roms = _context.Roms
                      .Where(r => r.TitleID == titleId);

        var hashes = roms.SelectMany(r => r.Hashes); // Flatten the list of hashes from all ROMs

        return hashes.ToList();
    }

    // Method to get a RomHash by its Value
    public RomHash GetRomHashByValue(string value)
    {
        return RomHashes.FirstOrDefault(rh => rh.Value == value);
    }

    // Method to get a collection of RomHashes by Filename
    public IEnumerable<RomHash> GetRomHashesByFilename(string filename)
    {
        return RomHashes.Where(rh => rh.Filename == filename);
    }

    // Method to get a collection of RomHashes by Extension
    public IEnumerable<RomHash> GetRomHashesByExtension(string extension)
    {
        return RomHashes.Where(rh => rh.Extension == extension);
    }

    // Method to get a collection of RomHashes by Filename and guaranteeing a specific RomHashProperty key or value
    public IEnumerable<RomHash> GetRomHashesByFilenameWithProperty(string filename, string key, string value)
    {
        return RomHashes.Where(rh => rh.Filename == filename && rh.Properties.Any(p => p.Key == key && p.Value == value));
    }

    // Method to get a collection of RomHashes by Extension and guaranteeing a specific RomHashProperty key or value
    public IEnumerable<RomHash> GetRomHashesByExtensionWithProperty(string extension, string key, string value)
    {
        return RomHashes.Where(rh => rh.Extension == extension && rh.Properties.Any(p => p.Key == key && p.Value == value));
    }
}


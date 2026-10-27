using System.Text.Json;
using RomManagerShared.Utils;

namespace RomManagerShared.Base;

/// <summary>One file or folder to put back: <see cref="From"/> is where it is now, <see cref="To"/> where it was.</summary>
public sealed record PathMove(string From, string To, bool IsDirectory);

/// <summary>A finished rename/move that can still be undone, as stored in the journal.</summary>
public sealed record JournalEntry(Guid Id, string Name, string Category, DateTime CompletedAt, List<PathMove> Undo);

/// <summary>
/// Persists the undo steps of finished rename/move operations so they can be undone after a restart.
/// Only path moves are stored; the library picks up the restored files on its next scan.
/// </summary>
public sealed class OperationJournal(string path, int maxEntries = 50)
{
    private readonly object _lock = new();
    private List<JournalEntry>? _entries;

    public IReadOnlyList<JournalEntry> Entries
    {
        get { lock (_lock) return [.. Load()]; }
    }

    public JournalEntry Add(string name, string category, List<PathMove> undo)
    {
        var entry = new JournalEntry(Guid.NewGuid(), name, category, DateTime.Now, undo);
        lock (_lock)
        {
            var entries = Load();
            entries.Add(entry);
            if (entries.Count > maxEntries) entries.RemoveRange(0, entries.Count - maxEntries);
            Save();
        }
        return entry;
    }

    public void Remove(Guid id)
    {
        lock (_lock)
        {
            if (Load().RemoveAll(e => e.Id == id) > 0) Save();
        }
    }

    private List<JournalEntry> Load()
    {
        if (_entries is not null) return _entries;
        try
        {
            _entries = File.Exists(path)
                ? JsonSerializer.Deserialize<List<JournalEntry>>(File.ReadAllText(path)) ?? []
                : [];
        }
        catch (Exception ex)
        {
            AppLog.Warn($"[Journal] {path}: {ex.Message}");
            _entries = [];
        }
        return _entries;
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(_entries));
            File.Move(tmp, path, overwrite: true);
        }
        catch (Exception ex) { AppLog.Warn($"[Journal] save failed: {ex.Message}"); }
    }
}

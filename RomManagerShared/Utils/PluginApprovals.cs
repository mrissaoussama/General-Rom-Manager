using System.Security.Cryptography;
using System.Text.Json;
using RomManagerShared.Base;

namespace RomManagerShared.Utils;

/// <summary>What the user is asked to approve before a plugin's code runs.</summary>
public sealed record PluginApprovalRequest(
    string Name, string Version, string? Author, string? Description,
    IReadOnlyList<PluginPermission> Permissions, string DllPath, string Sha256)
{
    public static string HashFile(string path)
    {
        using var stream = FileUtils.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}

/// <summary>
/// Plugins the user approved, pinned to the DLL's SHA-256 and the permissions shown at the time.
/// A changed DLL or a new permission needs approval again.
/// </summary>
public sealed class PluginApprovalStore(string path)
{
    private sealed record Entry(string Name, string Version, string Sha256, List<PluginPermission> Permissions);

    private readonly object _lock = new();
    private List<Entry>? _entries;

    public string Path { get; } = path;

    public bool IsApproved(PluginApprovalRequest request)
    {
        lock (_lock)
            return Load().Any(e => e.Name == request.Name
                && e.Sha256.Equals(request.Sha256, StringComparison.OrdinalIgnoreCase)
                && e.Permissions.Order().SequenceEqual(request.Permissions.Distinct().Order()));
    }

    public void Approve(PluginApprovalRequest request)
    {
        lock (_lock)
        {
            var entries = Load();
            entries.RemoveAll(e => e.Name == request.Name);
            entries.Add(new Entry(request.Name, request.Version, request.Sha256, [.. request.Permissions.Distinct()]));
            Save(entries);
        }
    }

    public void Revoke(string name)
    {
        lock (_lock)
        {
            var entries = Load();
            if (entries.RemoveAll(e => e.Name == name) > 0) Save(entries);
        }
    }

    private List<Entry> Load()
    {
        if (_entries is not null) return _entries;
        try
        {
            _entries = File.Exists(Path)
                ? JsonSerializer.Deserialize<List<Entry>>(File.ReadAllText(Path)) ?? []
                : [];
        }
        catch (Exception ex)
        {
            AppLog.Warn($"[Plugins] Cannot read {Path}: {ex.Message}");
            _entries = [];
        }
        return _entries;
    }

    private void Save(List<Entry> entries)
    {
        _entries = entries;
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        File.WriteAllText(Path, JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true }));
    }
}

using System.Buffers.Binary;
using System.Text;

namespace RomManagerShared.Utils;

/// <summary>
/// Minimal ISO9660 reader over cooked (2048), raw (2352, Mode 1/2) or CHD (2448 = raw + subcode) sector layouts.
/// </summary>
public sealed class RawCdReader
{
    // (frame size, user data offset) pairs; the right one has the "CD001" volume descriptor at LBA 16.
    private static readonly (int Frame, int Offset)[] Layouts =
        [(2048, 0), (2352, 24), (2352, 16), (2448, 0), (2448, 24), (2448, 16)];
    private readonly Stream _stream;
    private readonly int _sectorSize;
    private readonly int _dataOffset;

    public RawCdReader(Stream stream)
    {
        _stream = stream;
        (_sectorSize, _dataOffset) = Layouts[0];
        Span<byte> probe = stackalloc byte[6];
        foreach (var (frame, offset) in Layouts)
        {
            stream.Position = 16L * frame + offset;
            if (stream.ReadAtLeast(probe, probe.Length, throwOnEndOfStream: false) == probe.Length
                && probe[0] == 1 && probe[1..].SequenceEqual("CD001"u8))
            {
                (_sectorSize, _dataOffset) = (frame, offset);
                break;
            }
        }
    }

    public byte[] ReadSector(long lba)
    {
        var data = new byte[2048];
        _stream.Position = lba * _sectorSize + _dataOffset;
        _stream.ReadAtLeast(data, data.Length, throwOnEndOfStream: false);
        return data;
    }

    /// <summary>Reads a file from the root directory, or null when missing.</summary>
    public byte[]? ReadRootFile(string name)
    {
        foreach (var (entryName, lba, length) in RootEntries())
        {
            if (!entryName.Equals(name, StringComparison.OrdinalIgnoreCase)) continue;
            var size = (int)Math.Min(length, 0x10000);
            var file = new byte[size];
            for (int read = 0, i = 0; read < size; i++, read += 2048)
                ReadSector(lba + i).AsSpan(0, Math.Min(2048, size - read)).CopyTo(file.AsSpan(read));
            return file;
        }
        return null;
    }

    /// <summary>Names of the entries in the root directory (version suffix removed).</summary>
    public IEnumerable<string> RootFileNames() => RootEntries().Select(e => e.Name);

    private IEnumerable<(string Name, uint Lba, uint Length)> RootEntries()
    {
        var pvd = ReadSector(16);
        if (Encoding.ASCII.GetString(pvd, 1, 5) != "CD001") yield break;
        var rootLba = BinaryPrimitives.ReadUInt32LittleEndian(pvd.AsSpan(156 + 2));
        var rootSize = BinaryPrimitives.ReadUInt32LittleEndian(pvd.AsSpan(156 + 10));

        for (long sector = 0; sector * 2048 < rootSize; sector++)
        {
            var dir = ReadSector(rootLba + sector);
            for (int pos = 0; pos + 33 < 2048;)
            {
                int length = dir[pos];
                if (length == 0) break; // records never span sectors
                var nameLength = dir[pos + 32];
                var entryName = Encoding.ASCII.GetString(dir, pos + 33, Math.Min(nameLength, 2048 - pos - 33)).Split(';')[0];
                yield return (entryName,
                    BinaryPrimitives.ReadUInt32LittleEndian(dir.AsSpan(pos + 2)),
                    BinaryPrimitives.ReadUInt32LittleEndian(dir.AsSpan(pos + 10)));
                pos += length;
            }
        }
    }

    /// <summary>Boot executable name from SYSTEM.CNF ("BOOT = cdrom:\SLUS_005.55;1" or BOOT2 on PS2).</summary>
    public string? ReadBootExecutable()
    {
        var cnf = ReadRootFile("SYSTEM.CNF");
        if (cnf is null) return null;
        foreach (var line in Encoding.ASCII.GetString(cnf).Split('\n'))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("BOOT", StringComparison.OrdinalIgnoreCase)) continue;
            var value = trimmed[(trimmed.IndexOf('=') + 1)..].Trim();
            return value[(value.LastIndexOfAny([':', '\\', '/']) + 1)..].Split(';')[0];
        }
        return null;
    }
}

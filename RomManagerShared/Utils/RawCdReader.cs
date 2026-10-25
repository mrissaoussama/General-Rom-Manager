using System.Buffers.Binary;
using System.Text;

namespace RomManagerShared.Utils;

/// <summary>Minimal ISO9660 reader over 2048-byte or raw 2352-byte (Mode 1/2) CD images.</summary>
public sealed class RawCdReader
{
    private static readonly byte[] Sync = [0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00];
    private readonly Stream _stream;
    private readonly int _sectorSize;
    private readonly int _dataOffset;

    public RawCdReader(Stream stream)
    {
        _stream = stream;
        Span<byte> head = stackalloc byte[16];
        stream.Position = 0;
        if (stream.ReadAtLeast(head, 16, throwOnEndOfStream: false) == 16 && head[..12].SequenceEqual(Sync))
        {
            _sectorSize = 2352;
            _dataOffset = head[15] == 2 ? 24 : 16; // Mode 2 Form 1 has an 8-byte subheader
        }
        else
        {
            _sectorSize = 2048;
            _dataOffset = 0;
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
        var pvd = ReadSector(16);
        if (Encoding.ASCII.GetString(pvd, 1, 5) != "CD001") return null;
        var rootLba = BinaryPrimitives.ReadUInt32LittleEndian(pvd.AsSpan(156 + 2));
        var rootSize = BinaryPrimitives.ReadUInt32LittleEndian(pvd.AsSpan(156 + 10));

        for (long sector = 0; sector * 2048 < rootSize; sector++)
        {
            var dir = ReadSector(rootLba + sector);
            for (int pos = 0; pos < 2048;)
            {
                int length = dir[pos];
                if (length == 0) break; // records never span sectors
                var nameLength = dir[pos + 32];
                var entryName = Encoding.ASCII.GetString(dir, pos + 33, nameLength).Split(';')[0];
                if (entryName.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    var lba = BinaryPrimitives.ReadUInt32LittleEndian(dir.AsSpan(pos + 2));
                    var size = (int)Math.Min(BinaryPrimitives.ReadUInt32LittleEndian(dir.AsSpan(pos + 10)), 0x10000);
                    var file = new byte[size];
                    for (int read = 0, i = 0; read < size; i++, read += 2048)
                        ReadSector(lba + i).AsSpan(0, Math.Min(2048, size - read)).CopyTo(file.AsSpan(read));
                    return file;
                }
                pos += length;
            }
        }
        return null;
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

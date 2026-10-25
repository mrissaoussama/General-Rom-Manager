using System.Buffers.Binary;
using System.IO.Compression;

namespace RomManagerShared.Utils;

/// <summary>
/// Read-only seekable view of a CSO v1 (CISO, deflate) or ZSO (ZISO, LZ4) image as a plain ISO, so disc readers
/// work unchanged. Header: magic, header size, total size (u64), block size, version, index shift; then one u32
/// per block + 1.
/// </summary>
public sealed class CsoStream : Stream
{
    private const uint Magic = 0x4F534943; // "CISO"
    private const uint ZisoMagic = 0x4F53495A; // "ZISO"
    private const uint UncompressedFlag = 0x80000000;
    private readonly Stream _inner;
    private readonly uint[] _index;
    private readonly int _blockSize;
    private readonly int _shift;
    private readonly long _length;
    private long _position;
    private long _cachedBlock = -1;
    private readonly byte[] _cache;
    private readonly bool _lz4;

    private CsoStream(Stream inner, uint[] index, int blockSize, int shift, long length, bool lz4)
    {
        _lz4 = lz4;
        _inner = inner;
        _index = index;
        _blockSize = blockSize;
        _shift = shift;
        _length = length;
        _cache = new byte[blockSize];
    }

    /// <summary>Opens a CSO v1 or ZSO, or returns null when the stream isn't one (e.g. GameCube CISO).</summary>
    public static CsoStream? TryOpen(Stream inner)
    {
        Span<byte> header = stackalloc byte[0x18];
        inner.Position = 0;
        if (inner.ReadAtLeast(header, header.Length, throwOnEndOfStream: false) < header.Length) return null;
        var magic = BinaryPrimitives.ReadUInt32LittleEndian(header);
        if (magic is not (Magic or ZisoMagic)) return null;
        var headerSize = BinaryPrimitives.ReadUInt32LittleEndian(header[4..]);
        var totalSize = (long)BinaryPrimitives.ReadUInt64LittleEndian(header[8..]);
        var blockSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(header[0x10..]);
        var version = header[0x14];
        var shift = header[0x15];
        if (headerSize is not (0 or 0x18) || version > 1 || blockSize is < 0x800 or > 0x100000 || totalSize <= 0)
            return null;

        var blocks = (int)((totalSize + blockSize - 1) / blockSize);
        var raw = new byte[(blocks + 1) * 4];
        inner.ReadExactly(raw);
        var index = new uint[blocks + 1];
        for (int i = 0; i < index.Length; i++)
            index[i] = BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(i * 4));
        return new CsoStream(inner, index, blockSize, shift, totalSize, magic == ZisoMagic);
    }

    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        int total = 0;
        while (total < buffer.Length && _position < _length)
        {
            var block = _position / _blockSize;
            LoadBlock(block);
            var within = (int)(_position % _blockSize);
            var take = (int)Math.Min(Math.Min(buffer.Length - total, _blockSize - within), _length - _position);
            _cache.AsSpan(within, take).CopyTo(buffer[total..]);
            total += take;
            _position += take;
        }
        return total;
    }

    private void LoadBlock(long block)
    {
        if (block == _cachedBlock) return;
        var entry = _index[block];
        var start = (long)(entry & ~UncompressedFlag) << _shift;
        var end = (long)(_index[block + 1] & ~UncompressedFlag) << _shift;
        _inner.Position = start;
        if ((entry & UncompressedFlag) != 0)
        {
            _inner.ReadAtLeast(_cache, _blockSize, throwOnEndOfStream: false);
        }
        else
        {
            var compressed = new byte[end - start];
            _inner.ReadAtLeast(compressed, compressed.Length, throwOnEndOfStream: false);
            if (_lz4)
                Lz4DecodeBlock(compressed, _cache);
            else
            {
                using var deflate = new DeflateStream(new MemoryStream(compressed), CompressionMode.Decompress);
                deflate.ReadAtLeast(_cache, _blockSize, throwOnEndOfStream: false);
            }
        }
        _cachedBlock = block;
    }

    /// <summary>Decodes one raw LZ4 block (no frame); returns the bytes written.</summary>
    public static int Lz4DecodeBlock(ReadOnlySpan<byte> src, Span<byte> dst)
    {
        int s = 0, d = 0;
        while (s < src.Length)
        {
            var token = src[s++];
            var literals = token >> 4;
            if (literals == 15)
                for (byte b = 255; b == 255 && s < src.Length; literals += b) b = src[s++];
            literals = Math.Min(literals, Math.Min(src.Length - s, dst.Length - d));
            src.Slice(s, literals).CopyTo(dst[d..]);
            s += literals;
            d += literals;
            if (s + 2 > src.Length) break; // last sequence has literals only

            var offset = src[s] | src[s + 1] << 8;
            s += 2;
            var match = (token & 0x0F) + 4;
            if ((token & 0x0F) == 15)
                for (byte b = 255; b == 255 && s < src.Length; match += b) b = src[s++];
            if (offset == 0 || offset > d) throw new InvalidDataException("Bad LZ4 offset");
            match = Math.Min(match, dst.Length - d);
            for (int i = 0; i < match; i++, d++) dst[d] = dst[d - offset]; // may overlap
        }
        return d;
    }

    public override long Seek(long offset, SeekOrigin origin) => _position = origin switch
    {
        SeekOrigin.Begin => offset,
        SeekOrigin.Current => _position + offset,
        _ => _length + offset,
    };

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _length;
    public override long Position { get => _position; set => _position = value; }
    public override void Flush() { }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing) _inner.Dispose();
        base.Dispose(disposing);
    }
}

﻿using System;
using System.Runtime.CompilerServices;
using LibHac.Common;
using LibHac.Common.FixedArrays;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.Util;

namespace LibHac.Loader;

public class NsoReader
{
    private IFile NsoFile { get; set; }

    public NsoHeader Header;

    public Result Initialize(IFile nsoFile)
    {
        Result res = nsoFile.Read(out long bytesRead, 0, SpanHelpers.AsByteSpan(ref Header), ReadOption.None);
        if (res.IsFailure()) return res.Miss();

        if (bytesRead != Unsafe.SizeOf<NsoHeader>())
            return ResultLoader.InvalidNso.Log();

        NsoFile = nsoFile;
        return Result.Success;
    }

    public Result GetSegmentSize(SegmentType segment, out uint size)
    {
        UnsafeHelpers.SkipParamInit(out size);

        switch (segment)
        {
            case SegmentType.Text:
            case SegmentType.Ro:
            case SegmentType.Data:
                size = Header.Segments[(int)segment].Size;
                return Result.Success;
            default:
                return ResultLibHac.ArgumentOutOfRange.Log();
        }
    }

    public Result ReadSegment(SegmentType segment, Span<byte> buffer)
    {
        Result res = GetSegmentSize(segment, out uint segmentSize);
        if (res.IsFailure()) return res.Miss();

        if (buffer.Length < segmentSize)
            return ResultLibHac.BufferTooSmall.Log();

        bool isCompressed = (((int)Header.Flags >> (int)segment) & 1) != 0;
        bool checkHash = (((int)Header.Flags >> (int)segment) & 8) != 0;

        return ReadSegmentImpl(ref Header.Segments[(int)segment], Header.CompressedSizes[(int)segment],
            Header.SegmentHashes[(int)segment], isCompressed, checkHash, buffer);
    }

    private Result ReadSegmentImpl(ref NsoHeader.SegmentHeader segment, uint fileSize, Array32<byte> fileHash,
        bool isCompressed, bool checkHash, Span<byte> buffer)
    {
        // Select read size based on compression.
        if (!isCompressed)
        {
            fileSize = segment.Size;
        }

        // Validate size.
        if (fileSize > segment.Size)
            return ResultLoader.InvalidNso.Log();

        // Load data from file.
        uint loadAddress = isCompressed ? (uint)buffer.Length - fileSize : 0;

        Result res = NsoFile.Read(out long bytesRead, segment.FileOffset, buffer.Slice((int)loadAddress), ReadOption.None);
        if (res.IsFailure()) return res.Miss();

        if (bytesRead != fileSize)
            return ResultLoader.InvalidNso.Log();

        // Uncompress if necessary.
        if (isCompressed)
        {
            // todo: Fix in-place decompression
            // Lz4.Decompress(buffer.Slice((int)loadAddress), buffer);
            byte[] decomp = Lz4.Decompress(buffer.Slice((int)loadAddress).ToArray(), buffer.Length);
            decomp.CopyTo(buffer);
        }

        // Check hash if necessary.
        if (checkHash)
        {
            var hash = new Array32<byte>();
            Crypto.Sha256.GenerateSha256Hash(buffer.Slice(0, (int)segment.Size), hash);

            if (hash[..].SequenceCompareTo(fileHash) != 0)
                return ResultLoader.InvalidNso.Log();
        }

        return Result.Success;
    }

    public enum SegmentType
    {
        Text = 0,
        Ro = 1,
        Data = 2
    }
}
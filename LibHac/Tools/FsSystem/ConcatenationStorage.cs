﻿using System;
using System.Collections.Generic;
using LibHac.Fs;

namespace LibHac.Tools.FsSystem;

public class ConcatenationStorage : IStorage
{
    private ConcatSource[] Sources { get; }
    private long Length { get; }
    private bool LeaveOpen { get; }

    public ConcatenationStorage(IList<IStorage> sources, bool leaveOpen)
    {
        Sources = new ConcatSource[sources.Count];
        LeaveOpen = leaveOpen;

        long length = 0;
        for (int i = 0; i < sources.Count; i++)
        {
            sources[i].GetSize(out long sourceSize).ThrowIfFailure();

            if (sourceSize < 0) throw new ArgumentException("Sources must have an explicit length.");
            Sources[i] = new ConcatSource(sources[i], length, sourceSize);
            length += sourceSize;
        }

        Length = length;
    }

    public override Result Read(long offset, Span<byte> destination)
    {
        long inPos = offset;
        int outPos = 0;
        int remaining = destination.Length;

        Result res = CheckAccessRange(offset, destination.Length, Length);
        if (res.IsFailure()) return res.Miss();

        int sourceIndex = FindSource(inPos);

        while (remaining > 0)
        {
            ConcatSource entry = Sources[sourceIndex];
            long entryPos = inPos - entry.StartOffset;
            long entryRemain = entry.StartOffset + entry.Size - inPos;

            int bytesToRead = (int)Math.Min(entryRemain, remaining);

            res = entry.Storage.Read(entryPos, destination.Slice(outPos, bytesToRead));
            if (res.IsFailure()) return res.Miss();

            outPos += bytesToRead;
            inPos += bytesToRead;
            remaining -= bytesToRead;
            sourceIndex++;
        }

        return Result.Success;
    }

    public override Result Write(long offset, ReadOnlySpan<byte> source)
    {
        long inPos = offset;
        int outPos = 0;
        int remaining = source.Length;

        Result res = CheckAccessRange(offset, source.Length, Length);
        if (res.IsFailure()) return res.Miss();

        int sourceIndex = FindSource(inPos);

        while (remaining > 0)
        {
            ConcatSource entry = Sources[sourceIndex];
            long entryPos = inPos - entry.StartOffset;
            long entryRemain = entry.StartOffset + entry.Size - inPos;

            int bytesToWrite = (int)Math.Min(entryRemain, remaining);

            res = entry.Storage.Write(entryPos, source.Slice(outPos, bytesToWrite));
            if (res.IsFailure()) return res.Miss();

            outPos += bytesToWrite;
            inPos += bytesToWrite;
            remaining -= bytesToWrite;
            sourceIndex++;
        }

        return Result.Success;
    }

    public override Result Flush()
    {
        foreach (ConcatSource source in Sources)
        {
            Result res = source.Storage.Flush();
            if (res.IsFailure()) return res.Miss();
        }

        return Result.Success;
    }

    public override Result SetSize(long size)
    {
        return ResultFs.NotImplemented.Log();
    }

    public override Result GetSize(out long size)
    {
        size = Length;
        return Result.Success;
    }

    public override Result OperateRange(Span<byte> outBuffer, OperationId operationId, long offset, long size,
        ReadOnlySpan<byte> inBuffer)
    {
        throw new NotImplementedException();
    }

    public override void Dispose()
    {
        if (!LeaveOpen && Sources != null)
        {
            foreach (ConcatSource source in Sources)
            {
                source?.Storage?.Dispose();
            }
        }

        base.Dispose();
    }

    private int FindSource(long offset)
    {
        if (offset < 0 || offset >= Length)
            throw new ArgumentOutOfRangeException(nameof(offset), offset, "The Storage does not contain this offset.");

        int lo = 0;
        int hi = Sources.Length - 1;

        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);

            long val = Sources[mid].StartOffset;

            if (val == offset) return mid;

            if (val < offset)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return lo - 1;
    }

    private class ConcatSource
    {
        public IStorage Storage { get; }
        public long StartOffset { get; }
        public long Size { get; }

        public ConcatSource(IStorage storage, long startOffset, long length)
        {
            Storage = storage;
            StartOffset = startOffset;
            Size = length;
        }
    }
}
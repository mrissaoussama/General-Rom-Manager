﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LibHac.Fs;
using LibHac.FsSystem;

namespace LibHac.Tools.FsSystem;

public class Aes128CtrExStorage : Aes128CtrStorage
{
    public static readonly int NodeSize = 1024 * 16;

    private BucketTree Table { get; } = new BucketTree();

    private readonly object _locker = new object();

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct Entry
    {
        public long Offset;
        public int Reserved;
        public int Generation;
    }

    public Aes128CtrExStorage(IStorage baseStorage, SubStorage nodeStorage, SubStorage entryStorage,
        int entryCount, byte[] key, byte[] counter, bool leaveOpen)
        : base(baseStorage, key, counter, leaveOpen)
    {
        nodeStorage.GetSize(out long nodeStorageSize).ThrowIfFailure();
        entryStorage.GetSize(out long entryStorageSize).ThrowIfFailure();

        using var valueNodeStorage = new ValueSubStorage(nodeStorage, 0, nodeStorageSize);
        using var valueEntryStorage = new ValueSubStorage(entryStorage, 0, entryStorageSize);

        Result res = Table.Initialize(new ArrayPoolMemoryResource(), in valueNodeStorage, in valueEntryStorage, NodeSize,
            Unsafe.SizeOf<Entry>(), entryCount);
        res.ThrowIfFailure();
    }

    public override Result Read(long offset, Span<byte> destination)
    {
        if (destination.Length == 0)
            return Result.Success;

        Result res = Table.GetOffsets(out BucketTree.Offsets offsets);
        if (res.IsFailure()) return res.Miss();

        if (!offsets.IsInclude(offset, destination.Length))
            return ResultFs.OutOfRange.Log();

        using var visitor = new BucketTree.Visitor();

        res = Table.Find(ref visitor.Ref, offset);
        if (res.IsFailure()) return res.Miss();

        long inPos = offset;
        int outPos = 0;
        int remaining = destination.Length;

        while (remaining > 0)
        {
            var currentEntry = visitor.Get<Entry>();

            // Get and validate the next entry offset
            long nextEntryOffset;
            if (visitor.CanMoveNext())
            {
                res = visitor.MoveNext();
                if (res.IsFailure()) return res.Miss();

                nextEntryOffset = visitor.Get<Entry>().Offset;
                if (!offsets.IsInclude(nextEntryOffset))
                    return ResultFs.InvalidIndirectEntryOffset.Log();
            }
            else
            {
                nextEntryOffset = offsets.EndOffset;
            }

            int bytesToRead = (int)Math.Min(nextEntryOffset - inPos, remaining);

            lock (_locker)
            {
                UpdateCounterSubsection((uint)currentEntry.Generation);

                res = base.Read(inPos, destination.Slice(outPos, bytesToRead));
                if (res.IsFailure()) return res.Miss();
            }

            outPos += bytesToRead;
            inPos += bytesToRead;
            remaining -= bytesToRead;
        }

        return Result.Success;
    }

    public override Result Write(long offset, ReadOnlySpan<byte> source)
    {
        return ResultFs.UnsupportedWriteForAesCtrCounterExtendedStorage.Log();
    }

    public override Result Flush()
    {
        return Result.Success;
    }

    private void UpdateCounterSubsection(uint value)
    {
        Counter[7] = (byte)value;
        Counter[6] = (byte)(value >> 8);
        Counter[5] = (byte)(value >> 16);
        Counter[4] = (byte)(value >> 24);
    }
}
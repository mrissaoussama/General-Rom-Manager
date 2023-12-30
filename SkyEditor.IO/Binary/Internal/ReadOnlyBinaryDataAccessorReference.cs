﻿using System;
using System.Threading.Tasks;

namespace SkyEditor.IO.Binary
{
    /// <summary>
    /// Provides a view to a subset of a <see cref="IReadOnlyBinaryDataAccessor"/> or other <see cref="ReadOnlyBinaryDataAccessorReference"/>
    /// </summary>
    internal class ReadOnlyBinaryDataAccessorReference : IReadOnlyBinaryDataAccessor
    {
        public ReadOnlyBinaryDataAccessorReference(IReadOnlyBinaryDataAccessor data, ulong offset, long length)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            Data = data ?? throw new ArgumentNullException(nameof(data));
            Offset =(long) offset;
            Length = length;
        }

        public ReadOnlyBinaryDataAccessorReference(ReadOnlyBinaryDataAccessorReference reference, long offset, long length)
        {
            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference));
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            Data = reference.Data;
            Offset = reference.Offset + offset;
            Length = length;
        }

        private IReadOnlyBinaryDataAccessor Data { get; }

        private long Offset { get; set; }

        public long Length { get; private set; }

        public long Position { get; set; }

        public byte[] ReadArray()
        {
            if (Length > int.MaxValue)
            {
                throw new ArgumentException(Properties.Resources.Binary_ErrorLengthTooLarge);
            }

            return Data.ReadArray(Offset, (int)Length);
        }

        public ReadOnlySpan<byte> ReadSpan()
        {
            if (Length > int.MaxValue)
            {
                throw new ArgumentException(Properties.Resources.Binary_ErrorLengthTooLarge);
            }

            return Data.ReadSpan(Offset, (int)Length);
        }

        public async Task<byte[]> ReadArrayAsync()
        {
            if (Length > int.MaxValue)
            {
                throw new ArgumentException(Properties.Resources.Binary_ErrorLengthTooLarge);
            }

            return await Data.ReadArrayAsync(Offset, (int)Length);
        }

        public async Task<ReadOnlyMemory<byte>> ReadMemoryAsync()
        {
            if (Length > int.MaxValue)
            {
                throw new ArgumentException(Properties.Resources.Binary_ErrorLengthTooLarge);
            }

            return await Data.ReadMemoryAsync(Offset, (int)Length);
        }

        public byte ReadByte(long index)
        {
            return Data.ReadByte(Offset + index);
        }

        public async Task<byte> ReadByteAsync(long index)
        {
            return await Data.ReadByteAsync(Offset + index);
        }

        public byte[] ReadArray(long index, int length)
        {
            return Data.ReadArray(Offset + index, (int)Math.Min(Length, length));
        }

        public ReadOnlySpan<byte> ReadSpan(long index, int length)
        {
            return Data.ReadSpan(Offset + index, (int)Math.Min(Length, length));
        }

        public async Task<byte[]> ReadArrayAsync(long index, int length)
        {
            return await Data.ReadArrayAsync(Offset + index, (int)Math.Min(Length, length));
        }

        public async Task<ReadOnlyMemory<byte>> ReadMemoryAsync(long index, int length)
        {
            return await Data.ReadMemoryAsync(Offset + index, (int)Math.Min(Length, length));
        }
    }
}

﻿using System;
using System.Runtime.CompilerServices;
using LibHac.Common;
using LibHac.Diag;

namespace LibHac.Fs;

[NonCopyableDisposable]
public struct ValueSubStorage : IDisposable
{
    private IStorage _baseStorage;
    private long _offset;
    private long _size;
    private bool _isResizable;
    private SharedRef<IStorage> _sharedBaseStorage;

    public ValueSubStorage()
    {
        _baseStorage = null;
        _offset = 0;
        _size = 0;
        _isResizable = false;
        _sharedBaseStorage = new SharedRef<IStorage>();
    }

    public ValueSubStorage(ref readonly ValueSubStorage other)
    {
        _baseStorage = other._baseStorage;
        _offset = other._offset;
        _size = other._size;
        _isResizable = other._isResizable;
        _sharedBaseStorage = SharedRef<IStorage>.CreateCopy(in other._sharedBaseStorage);
    }

    public ValueSubStorage(IStorage baseStorage, long offset, long size)
    {
        _baseStorage = baseStorage;
        _offset = offset;
        _size = size;
        _isResizable = false;
        _sharedBaseStorage = new SharedRef<IStorage>();

        Assert.SdkRequiresNotNull(baseStorage);
        Assert.SdkRequiresLessEqual(0, offset);
        Assert.SdkRequiresLessEqual(0, size);
    }

    public ValueSubStorage(ref readonly ValueSubStorage subStorage, long offset, long size)
    {
        _baseStorage = subStorage._baseStorage;
        _offset = subStorage._offset + offset;
        _size = size;
        _isResizable = false;
        _sharedBaseStorage = SharedRef<IStorage>.CreateCopy(in subStorage._sharedBaseStorage);

        Assert.SdkRequiresLessEqual(0, offset);
        Assert.SdkRequiresLessEqual(0, size);
        Assert.SdkRequires(subStorage.IsValid());
        Assert.SdkRequiresGreaterEqual(subStorage._size, offset + size);
    }

    public ValueSubStorage(ref readonly SharedRef<IStorage> baseStorage, long offset, long size)
    {
        _baseStorage = baseStorage.Get;
        _offset = offset;
        _size = size;
        _isResizable = false;
        _sharedBaseStorage = SharedRef<IStorage>.CreateCopy(in baseStorage);

        Assert.SdkRequiresNotNull(in baseStorage);
        Assert.SdkRequiresLessEqual(0, _offset);
        Assert.SdkRequiresLessEqual(0, _size);
    }

    public void Dispose()
    {
        _baseStorage = null;
        _sharedBaseStorage.Destroy();
    }

    public readonly SubStorage GetSubStorage()
    {
        if (_sharedBaseStorage.HasValue)
        {
            return new SubStorage(in _sharedBaseStorage, _offset, _size);
        }

        return new SubStorage(_baseStorage, _offset, _size);
    }

    public void Set(ref readonly ValueSubStorage other)
    {
        if (!Unsafe.AreSame(ref Unsafe.AsRef(in this), ref Unsafe.AsRef(in other)))
        {
            _baseStorage = other._baseStorage;
            _offset = other._offset;
            _size = other._size;
            _isResizable = other._isResizable;
            _sharedBaseStorage.SetByCopy(in other._sharedBaseStorage);
        }
    }

    public void Set(in ValueSubStorage other, long offset, long size)
    {
        if (!Unsafe.AreSame(ref Unsafe.AsRef(in this), ref Unsafe.AsRef(in other)))
        {
            _baseStorage = other._baseStorage;
            _offset = other._offset + offset;
            _size = size;
            _isResizable = false;
            _sharedBaseStorage.SetByCopy(in other._sharedBaseStorage);

            Assert.SdkRequiresLessEqual(0, offset);
            Assert.SdkRequiresLessEqual(0, size);
            Assert.SdkRequires(other.IsValid());
            Assert.SdkRequiresGreaterEqual(other._size, offset + size);
        }
    }

    private readonly bool IsValid() => _baseStorage is not null;

    public void SetResizable(bool isResizable)
    {
        _isResizable = isResizable;
    }

    public readonly Result Read(long offset, Span<byte> destination)
    {
        if (!IsValid()) return ResultFs.NotInitialized.Log();
        if (destination.Length == 0) return Result.Success;

        Result res = IStorage.CheckAccessRange(offset, destination.Length, _size);
        if (res.IsFailure()) return res.Miss();

        res = _baseStorage.Read(_offset + offset, destination);
        if (res.IsFailure()) return res.Miss();

        return Result.Success;
    }

    public readonly Result Write(long offset, ReadOnlySpan<byte> source)
    {
        if (!IsValid()) return ResultFs.NotInitialized.Log();
        if (source.Length == 0) return Result.Success;

        Result res = IStorage.CheckAccessRange(offset, source.Length, _size);
        if (res.IsFailure()) return res.Miss();

        res = _baseStorage.Write(_offset + offset, source);
        if (res.IsFailure()) return res.Miss();

        return Result.Success;
    }

    public readonly Result Flush()
    {
        if (!IsValid()) return ResultFs.NotInitialized.Log();

        Result res = _baseStorage.Flush();
        if (res.IsFailure()) return res.Miss();

        return Result.Success;
    }

    public Result SetSize(long size)
    {
        if (!IsValid()) return ResultFs.NotInitialized.Log();
        if (!_isResizable) return ResultFs.UnsupportedSetSizeForNotResizableSubStorage.Log();

        Result res = IStorage.CheckOffsetAndSize(_offset, size);
        if (res.IsFailure()) return res.Miss();

        res = _baseStorage.GetSize(out long currentSize);
        if (res.IsFailure()) return res.Miss();

        if (currentSize != _offset + _size)
        {
            // SubStorage cannot be resized unless it is located at the end of the base storage.
            return ResultFs.UnsupportedSetSizeForResizableSubStorage.Log();
        }

        res = _baseStorage.SetSize(_offset + size);
        if (res.IsFailure()) return res.Miss();

        _size = size;

        return Result.Success;
    }

    public readonly Result GetSize(out long size)
    {
        UnsafeHelpers.SkipParamInit(out size);

        if (!IsValid()) return ResultFs.NotInitialized.Log();

        size = _size;
        return Result.Success;
    }

    public readonly Result OperateRange(OperationId operationId, long offset, long size)
    {
        return OperateRange(Span<byte>.Empty, operationId, offset, size, ReadOnlySpan<byte>.Empty);
    }

    public readonly Result OperateRange(Span<byte> outBuffer, OperationId operationId, long offset, long size, ReadOnlySpan<byte> inBuffer)
    {
        if (!IsValid()) return ResultFs.NotInitialized.Log();

        if (operationId != OperationId.InvalidateCache)
        {
            if (size == 0) return Result.Success;

            Result res = IStorage.CheckOffsetAndSize(_offset, size);
            if (res.IsFailure()) return res.Miss();
        }

        return _baseStorage.OperateRange(outBuffer, operationId, _offset + offset, size, inBuffer);
    }
}
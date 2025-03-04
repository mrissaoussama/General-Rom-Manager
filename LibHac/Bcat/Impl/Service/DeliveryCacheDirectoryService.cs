﻿using System;
using LibHac.Bcat.Impl.Ipc;
using LibHac.Bcat.Impl.Service.Core;
using LibHac.Common;

namespace LibHac.Bcat.Impl.Service;

internal class DeliveryCacheDirectoryService : IDeliveryCacheDirectoryService
{
    private BcatServer Server { get; }
    private object Locker { get; } = new object();
    private DeliveryCacheStorageService Parent { get; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    private AccessControl Access { get; }
    private ulong ApplicationId { get; }
    private DirectoryName _name;
    private bool IsDirectoryOpen { get; set; }
    private int Count { get; set; }

    public DeliveryCacheDirectoryService(BcatServer server, DeliveryCacheStorageService parent, ulong applicationId,
        AccessControl accessControl)
    {
        Server = server;
        Parent = parent;
        ApplicationId = applicationId;
        Access = accessControl;
    }

    public Result Open(ref readonly DirectoryName name)
    {
        if (!name.IsValid())
            return ResultBcat.InvalidArgument.Log();

        lock (Locker)
        {
            if (IsDirectoryOpen)
                return ResultBcat.AlreadyOpen.Log();

            var metaReader = new DeliveryCacheFileMetaAccessor(Server);
            Result res = metaReader.ReadApplicationFileMeta(ApplicationId, in name, false);
            if (res.IsFailure()) return res.Miss();

            Count = metaReader.Count;
            _name = name;
            IsDirectoryOpen = true;

            return Result.Success;
        }
    }

    public Result Read(out int entriesRead, Span<DeliveryCacheDirectoryEntry> entryBuffer)
    {
        UnsafeHelpers.SkipParamInit(out entriesRead);

        lock (Locker)
        {
            if (!IsDirectoryOpen)
                return ResultBcat.NotOpen.Log();

            var metaReader = new DeliveryCacheFileMetaAccessor(Server);
            Result res = metaReader.ReadApplicationFileMeta(ApplicationId, in _name, true);
            if (res.IsFailure()) return res.Miss();

            int i;
            for (i = 0; i < entryBuffer.Length; i++)
            {
                res = metaReader.GetEntry(out DeliveryCacheFileMetaEntry entry, i);

                if (res.IsFailure())
                {
                    if (!ResultBcat.NotFound.Includes(res))
                        return res;

                    break;
                }

                entryBuffer[i] = new DeliveryCacheDirectoryEntry(in entry.Name, entry.Size, in entry.Digest);
            }

            entriesRead = i;
            return Result.Success;
        }
    }

    public Result GetCount(out int count)
    {
        UnsafeHelpers.SkipParamInit(out count);

        lock (Locker)
        {
            if (!IsDirectoryOpen)
            {
                return ResultBcat.NotOpen.Log();
            }

            count = Count;
            return Result.Success;
        }
    }

    public void Dispose()
    {
        Parent.NotifyCloseDirectory();
    }
}
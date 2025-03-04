﻿// ReSharper disable UnusedMember.Local UnusedType.Local
#pragma warning disable CS0169 // Field is never used
using System;
using System.Collections.Generic;
using LibHac.FsSrv.Sf;
using LibHac.Os;

namespace LibHac.FsSrv.Impl;

public abstract class Prohibitee : IDisposable
{
    // IntrusiveList
    private bool _isRegistered;
    private SaveDataPorterManager _porterManager;

    public Prohibitee(SaveDataPorterManager porterManager)
    {
        throw new NotImplementedException();
    }

    public virtual void Dispose()
    {
        throw new NotImplementedException();
    }

    public Result Initialize()
    {
        throw new NotImplementedException();
    }

    public void Unregister()
    {
        throw new NotImplementedException();
    }

    public abstract void Invalidate();
    public abstract ApplicationId GetApplicationId();
}

public class SaveDataPorterProhibiter : ISaveDataTransferProhibiter
{
    // IntrusiveList
    private SaveDataPorterManager _porterManager;
    private Ncm.ApplicationId _applicationId;

    public SaveDataPorterProhibiter(SaveDataPorterManager porterManager, Ncm.ApplicationId applicationId)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public Ncm.ApplicationId GetApplicationId()
    {
        throw new NotImplementedException();
    }
}

public class SaveDataPorterManager
{
    private LinkedList<Prohibitee> _prohibiteeList;
    private LinkedList<SaveDataPorterProhibiter> _porterProhibiterList;
    private SdkMutex _mutex;

    public SaveDataPorterManager()
    {
        _prohibiteeList = new LinkedList<Prohibitee>();
        _porterProhibiterList = new LinkedList<SaveDataPorterProhibiter>();
        _mutex = new SdkMutex();
    }

    public bool IsProhibited(ref UniqueLock<SdkMutex> refLock, ApplicationId applicationId)
    {
        throw new NotImplementedException();
    }

    public bool RegisterPorter(Prohibitee prohibitee, ApplicationId applicationId)
    {
        throw new NotImplementedException();
    }

    public void UnregisterPorter(Prohibitee prohibitee)
    {
        throw new NotImplementedException();
    }

    public void RegisterProhibiter(SaveDataPorterProhibiter porterProhibiter)
    {
        throw new NotImplementedException();
    }

    public void UnregisterProhibiter(SaveDataPorterProhibiter porterProhibiter)
    {
        throw new NotImplementedException();
    }
}
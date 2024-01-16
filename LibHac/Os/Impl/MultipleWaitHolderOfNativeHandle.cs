namespace LibHac.Os.Impl;

public class MultiWaitHolderOfNativeHandle : MultiWaitHolderOfNativeWaitObject
{
    private readonly OsNativeHandle _handle;

    internal MultiWaitHolderOfNativeHandle(OsNativeHandle handle)
    {
        _handle = handle;
    }

    public override TriBool IsSignaled()
    {
        return TriBool.Undefined;
    }

    public override bool GetNativeHandle(out OsNativeHandle handle)
    {
        handle = _handle;
        return false;
    }
}
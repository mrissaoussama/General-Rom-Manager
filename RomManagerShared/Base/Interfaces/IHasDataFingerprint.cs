namespace RomManagerShared.Interfaces;

/// <summary>
/// A title info provider whose results depend on data files the user can change (e.g. DATs).
/// When the fingerprint changes, the library scanner re-parses that console's cached files.
/// </summary>
public interface IHasDataFingerprint
{
    /// <summary>Changes whenever the data files change; empty when there are none.</summary>
    string DataFingerprint { get; }
}

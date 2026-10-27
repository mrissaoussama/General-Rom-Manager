using RomManagerShared.Utils;

namespace RomManagerShared.Interfaces;

/// <summary>
/// Implemented by console modules that need files from the internet (title databases, version lists). The app lists
/// what is missing (Settings → Download title databases) and queues the downloads. Found automatically, like parsers.
/// </summary>
public interface IDownloadableResources
{
    /// <summary>One entry per missing file, each with one URL and one local path.</summary>
    IEnumerable<DownloadMethodInfo> GetMissingResources();
}

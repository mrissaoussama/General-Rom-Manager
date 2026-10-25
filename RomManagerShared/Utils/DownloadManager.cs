using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Collections.Generic;
using System.IO;
using AltoMultiThreadDownloadManager;
using AltoMultiThreadDownloadManager.EventArguments;
namespace RomManagerShared.Utils;
public class DownloadManager
{
    private static readonly List<HttpMultiThreadDownloader> _downloads;
    private static readonly string _interruptedDownloadsFile;

    public static event EventHandler<HttpMultiThreadDownloader> DownloadStarted;

     static DownloadManager()
    {
        _downloads = new List<HttpMultiThreadDownloader>();
        LoadInterruptedDownloads();
    }

    public static void StartDownload(string url, string saveFolder, string saveFileName, string chunkFilesFolder, int nofMaxThread)
    {
        var downloader = new HttpMultiThreadDownloader(url, saveFolder, saveFileName, chunkFilesFolder, nofMaxThread);
        downloader.ProgressChanged += Downloader_ProgressChanged;
        downloader.DownloadInfoReceived += Downloader_DownloadInfoReceived;
        downloader.Completed += Downloader_Completed;
        downloader.StatusChanged += Downloader_StatusChanged;
        downloader.ErrorOccured += Downloader_ErrorOccured;
        downloader.Stopped += Downloader_Stopped;
        downloader.MergeCompleted += Downloader_MergeCompleted;
        downloader.MergingProgressChanged += Downloader_MergingProgressChanged;
        _downloads.Add(downloader);
        OnDownloadStarted(downloader);
        downloader.Start();
    }

    private static void Downloader_MergingProgressChanged(object? sender, MergingProgressChangedEventArgs e)
    {
    }

    private static void Downloader_MergeCompleted(object? sender, EventArgs e)
    {
    }

    private static void Downloader_Stopped(object? sender, EventArgs e)
    {
    }

    private static void Downloader_ErrorOccured(object? sender, ErrorEventArgs e)
    {
        FileUtils.Log($"Download error: {e}");
    }

    public static void PauseDownload(HttpMultiThreadDownloader downloader)
    {
        downloader.Stop();
    }

    public static void ResumeDownload(HttpMultiThreadDownloader downloader)
    {
        downloader.Resume();
    }

    private static void Downloader_ProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        // Update progress UI
    }

    private static void Downloader_DownloadInfoReceived(object sender, EventArgs e)
    {
        var downloader = (HttpMultiThreadDownloader)sender;
        if (!FileUtils.DriveHasEnoughSpace(downloader.Info.ContentSize,downloader.SaveDir))
        {
            PauseDownload(downloader);
        }
        if (!FileUtils.DriveHasEnoughSpace(downloader.Info.ContentSize, downloader.RangeDir))
        {
            PauseDownload(downloader);
        }
    }

    private static void Downloader_StatusChanged(object sender, StatusChangedEventArgs e)
    {
        var downloader = (HttpMultiThreadDownloader)sender;
    }

    private static void Downloader_Completed(object sender, EventArgs e)
    {
        var downloader = (HttpMultiThreadDownloader)sender;
    }

    private static void LoadInterruptedDownloads()
    {
        if (File.Exists(_interruptedDownloadsFile))
        {
            // Load interrupted downloads from file and resume them
            // Example: Read interrupted download information from file and call StartDownload method
        }
    }

    private static void SaveInterruptedDownload(HttpMultiThreadDownloader downloader)
    {
        // Save information about the interrupted download to a file
        // Example: Serialize downloader.Info and save it to _interruptedDownloadsFile
    }

    public  static void OnDownloadStarted(HttpMultiThreadDownloader downloader)
    {
        DownloadStarted?.Invoke(null, downloader);
    }
}




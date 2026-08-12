using CommunityToolkit.Mvvm.ComponentModel;
using TorrentApp.Models;

namespace TorrentApp.ViewModels;

/// <summary>
/// Observable wrapper around a <see cref="TorrentItem"/> for display in the torrent list.
/// Updated in-place by <see cref="DownloadsViewModel"/> to avoid recreating list items.
/// </summary>
public sealed partial class TorrentItemViewModel : ObservableObject
{
    // The underlying model — never exposed directly to Views.
    private readonly TorrentItem _item;

    public TorrentItemViewModel(TorrentItem item)
    {
        _item = item;
        SyncFromModel();
    }

    // -----------------------------------------------------------------------
    // Observable properties bound by the View
    // -----------------------------------------------------------------------

    [ObservableProperty]
    public partial string InfoHash { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial TorrentStatus Status { get; set; }

    [ObservableProperty]
    public partial double Progress { get; set; }

    [ObservableProperty]
    public partial string DownloadSpeed { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string UploadSpeed { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Eta { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Downloaded { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TotalSize { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int Peers { get; set; }

    [ObservableProperty]
    public partial int Seeds { get; set; }

    [ObservableProperty]
    public partial string StatusText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SavePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<TorrentFileInfo> Files { get; set; } = [];

    // -----------------------------------------------------------------------
    // Computed display properties
    // -----------------------------------------------------------------------

    public bool IsDownloading => Status == TorrentStatus.Downloading;
    public bool IsPaused => Status is TorrentStatus.Paused or TorrentStatus.Queued;
    public bool IsCompleted => Status is TorrentStatus.Completed or TorrentStatus.Seeding;
    public bool IsError => Status == TorrentStatus.Error;
    public bool IsFetchingMetadata => Status == TorrentStatus.FetchingMetadata;
    public bool CanStart => Status is TorrentStatus.Paused or TorrentStatus.Queued or TorrentStatus.Error;
    public bool CanPause => Status is TorrentStatus.Downloading or TorrentStatus.Seeding or TorrentStatus.FetchingMetadata;

    // -----------------------------------------------------------------------
    // Sync from model (called by polling timer)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Updates all observable properties from the underlying model.
    /// Only changed properties raise PropertyChanged to minimize binding churn.
    /// </summary>
    public void SyncFromModel()
    {
        InfoHash = _item.InfoHash;
        Name = _item.Name;
        Status = _item.Status;
        Progress = _item.Progress;
        DownloadSpeed = FormatSpeed(_item.DownloadSpeedBps);
        UploadSpeed = FormatSpeed(_item.UploadSpeedBps);
        Eta = FormatEta(_item.Eta);
        Downloaded = FormatBytes(_item.DownloadedBytes);
        TotalSize = FormatBytes(_item.TotalBytes);
        Peers = _item.Peers;
        Seeds = _item.Seeds;
        StatusText = GetStatusText(_item.Status);
        SavePath = _item.SavePath;
        ErrorMessage = _item.ErrorMessage ?? string.Empty;
        Files = _item.Files;

        OnPropertyChanged(nameof(IsDownloading));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(IsCompleted));
        OnPropertyChanged(nameof(IsError));
        OnPropertyChanged(nameof(IsFetchingMetadata));
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(CanPause));
    }

    // -----------------------------------------------------------------------
    // Formatting helpers
    // -----------------------------------------------------------------------

    private static string FormatSpeed(long bps)
    {
        if (bps <= 0)
        {
            return "0 KB/s";
        }

        if (bps < 1_000_000)
        {
            return $"{bps / 1024.0:F1} KB/s";
        }

        if (bps < 1_000_000_000)
        {
            return $"{bps / 1_048_576.0:F1} MB/s";
        }

        return $"{bps / 1_073_741_824.0:F2} GB/s";
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0)
        {
            return "0 B";
        }

        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        if (bytes < 1_048_576)
        {
            return $"{bytes / 1024.0:F1} KB";
        }

        if (bytes < 1_073_741_824)
        {
            return $"{bytes / 1_048_576.0:F1} MB";
        }

        return $"{bytes / 1_073_741_824.0:F2} GB";
    }

    private static string FormatEta(TimeSpan? eta)
    {
        if (eta is null)
        {
            return "–";
        }

        if (eta.Value.TotalHours >= 1)
        {
            return $"{(int)eta.Value.TotalHours:D2}:{eta.Value.Minutes:D2}:{eta.Value.Seconds:D2}";
        }

        return $"{eta.Value.Minutes:D2}:{eta.Value.Seconds:D2}";
    }

    private static string GetStatusText(TorrentStatus status) => status switch
    {
        TorrentStatus.Queued => "Queued",
        TorrentStatus.Checking => "Checking files…",
        TorrentStatus.Downloading => "Downloading",
        TorrentStatus.Paused => "Paused",
        TorrentStatus.Completed => "Completed",
        TorrentStatus.Seeding => "Seeding",
        TorrentStatus.Error => "Error",
        TorrentStatus.FetchingMetadata => "Getting metadata…",
        TorrentStatus.Stopping => "Stopping…",
        _ => "Unknown",
    };
}

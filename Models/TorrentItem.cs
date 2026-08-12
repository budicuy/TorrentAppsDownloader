namespace TorrentApp.Models;

/// <summary>
/// Application-owned representation of a torrent and its current state.
/// Does not expose any MonoTorrent types directly.
/// </summary>
public sealed class TorrentItem
{
    /// <summary>Stable identifier — the torrent info-hash hex string.</summary>
    public string InfoHash { get; init; } = string.Empty;

    /// <summary>Display name derived from the torrent metadata.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Current lifecycle state of the torrent.</summary>
    public TorrentStatus Status { get; set; } = TorrentStatus.Queued;

    /// <summary>Download progress in the range [0.0, 1.0].</summary>
    public double Progress { get; set; }

    /// <summary>Current download speed in bytes per second.</summary>
    public long DownloadSpeedBps { get; set; }

    /// <summary>Current upload speed in bytes per second.</summary>
    public long UploadSpeedBps { get; set; }

    /// <summary>Total number of bytes to download.</summary>
    public long TotalBytes { get; set; }

    /// <summary>Total bytes downloaded so far.</summary>
    public long DownloadedBytes { get; set; }

    /// <summary>Total bytes uploaded during this session.</summary>
    public long UploadedBytes { get; set; }

    /// <summary>Number of connected peers.</summary>
    public int Peers { get; set; }

    /// <summary>Number of connected seeds.</summary>
    public int Seeds { get; set; }

    /// <summary>Estimated time remaining (null when unknown or complete).</summary>
    public TimeSpan? Eta { get; set; }

    /// <summary>Absolute path to the download directory for this torrent.</summary>
    public string SavePath { get; set; } = string.Empty;

    /// <summary>Human-readable error message when Status == Error.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Files included in this torrent.</summary>
    public IReadOnlyList<TorrentFileInfo> Files { get; set; } = [];

    /// <summary>When the torrent was added to the client.</summary>
    public DateTimeOffset AddedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Share ratio (uploaded / downloaded). Returns 0 when nothing has been downloaded.</summary>
    public double Ratio => DownloadedBytes > 0 ? (double)UploadedBytes / DownloadedBytes : 0.0;
}

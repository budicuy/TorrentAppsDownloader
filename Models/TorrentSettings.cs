namespace TorrentApp.Models;

/// <summary>
/// Persisted application settings. Serialized as JSON to local app data.
/// </summary>
public sealed class TorrentSettings
{
    /// <summary>Default save directory for new torrents.</summary>
    public string DefaultSavePath { get; set; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads",
            "Torrents");

    /// <summary>Global download speed limit in bytes/sec. 0 = unlimited.</summary>
    public int MaxDownloadSpeedBps { get; set; }

    /// <summary>Global upload speed limit in bytes/sec. 0 = unlimited.</summary>
    public int MaxUploadSpeedBps { get; set; }

    /// <summary>Application theme preference: System, Light, or Dark.</summary>
    public string Theme { get; set; } = "System";

    /// <summary>Maximum number of active torrents downloading simultaneously.</summary>
    public int MaxActiveTorrents { get; set; } = 5;

    /// <summary>Port used by MonoTorrent for incoming connections. 0 = random.</summary>
    public int ListenPort { get; set; } = 0;
}

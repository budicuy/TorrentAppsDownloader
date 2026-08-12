namespace TorrentApp.Models;

/// <summary>
/// Represents the lifecycle state of a torrent managed by the application.
/// </summary>
public enum TorrentStatus
{
    /// <summary>Torrent is queued and waiting to start.</summary>
    Queued,

    /// <summary>Torrent engine is verifying existing downloaded pieces.</summary>
    Checking,

    /// <summary>Torrent is actively downloading pieces from peers.</summary>
    Downloading,

    /// <summary>Download is paused; no peer connections are maintained.</summary>
    Paused,

    /// <summary>All pieces have been downloaded successfully.</summary>
    Completed,

    /// <summary>An unrecoverable error has occurred.</summary>
    Error,

    /// <summary>Torrent metadata is being fetched from the DHT / peers.</summary>
    FetchingMetadata,

    /// <summary>Torrent is seeding (uploading only — fully downloaded).</summary>
    Seeding,

    /// <summary>Torrent is stopping.</summary>
    Stopping,
}

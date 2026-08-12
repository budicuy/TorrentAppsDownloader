using TorrentApp.Models;

namespace TorrentApp.Services;

/// <summary>
/// Abstraction layer over the BitTorrent engine.
/// ViewModels depend on this interface, never on MonoTorrent types directly.
/// </summary>
public interface ITorrentService : IAsyncDisposable
{
    /// <summary>Fired when any torrent's state has changed meaningfully.</summary>
    event EventHandler<TorrentItem>? TorrentStateChanged;

    /// <summary>Fired when a new torrent has been added to the engine.</summary>
    event EventHandler<TorrentItem>? TorrentAdded;

    /// <summary>Fired when a torrent has been removed from the engine.</summary>
    event EventHandler<string>? TorrentRemoved;

    /// <summary>Fired when a torrent finishes downloading.</summary>
    event EventHandler<TorrentItem>? TorrentCompleted;

    /// <summary>Returns a snapshot of all currently managed torrents.</summary>
    IReadOnlyList<TorrentItem> GetAllTorrents();

    /// <summary>
    /// Starts the engine, loads persisted state, and resumes any previously active torrents.
    /// Must be called once during application startup.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a torrent from a .torrent file on disk.
    /// Returns the resulting <see cref="TorrentItem"/> or throws on failure.
    /// </summary>
    Task<TorrentItem> AddTorrentFileAsync(
        string torrentFilePath,
        string savePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a torrent from a magnet URI.
    /// The method returns quickly; metadata will be retrieved asynchronously.
    /// </summary>
    Task<TorrentItem> AddMagnetLinkAsync(
        string magnetUri,
        string savePath,
        CancellationToken cancellationToken = default);

    /// <summary>Starts or resumes downloading the specified torrent.</summary>
    Task StartAsync(string infoHash, CancellationToken cancellationToken = default);

    /// <summary>Pauses the specified torrent.</summary>
    Task PauseAsync(string infoHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the specified torrent from the engine.
    /// </summary>
    /// <param name="infoHash">Torrent identifier.</param>
    /// <param name="deleteFiles">When true, downloaded data is also deleted.</param>
    Task RemoveAsync(
        string infoHash,
        bool deleteFiles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies global speed limits from the current settings.
    /// </summary>
    Task ApplySpeedLimitsAsync(int maxDownloadBps, int maxUploadBps);

    /// <summary>
    /// Persists all engine state (fast-resume data, torrent list) so it can
    /// be restored on the next application start.
    /// </summary>
    Task SaveStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies live engine statistics into the app-owned <see cref="TorrentItem"/> models.
    /// Designed to be called every ~500 ms from the UI thread's DispatcherTimer.
    /// </summary>
    void RefreshStatistics();
}

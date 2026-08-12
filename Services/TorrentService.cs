using Microsoft.Extensions.Logging;
using MonoTorrent;
using MonoTorrent.Client;
using TorrentApp.Models;
using Windows.Storage;

namespace TorrentApp.Services;

/// <summary>
/// MonoTorrent 3.0.2-backed implementation of <see cref="ITorrentService"/>.
/// All MonoTorrent types are fully contained within this class — nothing leaks out.
/// </summary>
internal sealed class TorrentService : ITorrentService
{
    // -----------------------------------------------------------------------
    // Events (ITorrentService)
    // -----------------------------------------------------------------------

    public event EventHandler<TorrentItem>? TorrentStateChanged;
    public event EventHandler<TorrentItem>? TorrentAdded;
    public event EventHandler<string>? TorrentRemoved;
    public event EventHandler<TorrentItem>? TorrentCompleted;

    // -----------------------------------------------------------------------
    // Private state
    // -----------------------------------------------------------------------

    private readonly ILogger<TorrentService> _logger;
    private readonly string _dataFolder;        // engine cache + fast-resume
    private readonly string _torrentFileFolder; // cached .torrent files

    private ClientEngine? _engine;
    private readonly Dictionary<string, TorrentManager> _managers = [];
    private readonly Dictionary<string, TorrentItem> _items = [];
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;

    // -----------------------------------------------------------------------
    // Constructor
    // -----------------------------------------------------------------------

    public TorrentService(ILogger<TorrentService> logger)
    {
        _logger = logger;
        string localFolder = GetLocalDataFolder();
        _dataFolder = Path.Combine(localFolder, "engine");
        _torrentFileFolder = Path.Combine(_dataFolder, "torrents");
    }

    private static string GetLocalDataFolder()
    {
        try
        {
            return ApplicationData.Current.LocalFolder.Path;
        }
        catch
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string folder = Path.Combine(appData, "TorrentApp");
            Directory.CreateDirectory(folder);
            return folder;
        }
    }

    // -----------------------------------------------------------------------
    // ITorrentService — Initialize
    // -----------------------------------------------------------------------

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_dataFolder);
        Directory.CreateDirectory(_torrentFileFolder);

        var settings = new EngineSettingsBuilder
        {
            CacheDirectory = _dataFolder,
            AllowPortForwarding = true,
            AutoSaveLoadFastResume = true,
            AutoSaveLoadMagnetLinkMetadata = true,
        }.ToSettings();

        _engine = new ClientEngine(settings);

        _logger.LogInformation(
            "MonoTorrent engine initialized. Cache: {Folder}", _dataFolder);

        await RestoreStateAsync(cancellationToken).ConfigureAwait(false);
    }

    // -----------------------------------------------------------------------
    // ITorrentService — GetAllTorrents
    // -----------------------------------------------------------------------

    public IReadOnlyList<TorrentItem> GetAllTorrents()
    {
        lock (_items)
        {
            return [.. _items.Values];
        }
    }

    // -----------------------------------------------------------------------
    // ITorrentService — AddTorrentFile
    // -----------------------------------------------------------------------

    public async Task<TorrentItem> AddTorrentFileAsync(
        string torrentFilePath,
        string savePath,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        _logger.LogInformation("Adding .torrent file: {Path}", torrentFilePath);

        Torrent torrent = await Torrent.LoadAsync(torrentFilePath).ConfigureAwait(false);
        string infoHash = torrent.InfoHashes.V1OrV2.ToHex();

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_managers.ContainsKey(infoHash))
            {
                _logger.LogWarning("Torrent {Hash} already added.", infoHash);
                return _items[infoHash];
            }

            Directory.CreateDirectory(savePath);

            // Copy .torrent into our managed folder for restoration on restart.
            string dest = Path.Combine(_torrentFileFolder, infoHash + ".torrent");
            if (!File.Exists(dest))
            {
                File.Copy(torrentFilePath, dest, overwrite: false);
            }

            TorrentManager manager = await _engine!.AddAsync(torrent, savePath)
                .ConfigureAwait(false);

            TorrentItem item = BuildItem(manager, infoHash, torrent.Name, savePath);
            RegisterManager(manager, item);

            TorrentAdded?.Invoke(this, item);
            _logger.LogInformation(
                "Torrent added: {Name} [{Hash}]", item.Name, infoHash);
            return item;
        }
        finally
        {
            _lock.Release();
        }
    }

    // -----------------------------------------------------------------------
    // ITorrentService — AddMagnetLink
    // -----------------------------------------------------------------------

    public async Task<TorrentItem> AddMagnetLinkAsync(
        string magnetUri,
        string savePath,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        _logger.LogInformation("Adding magnet link.");

        MagnetLink magnet = MagnetLink.Parse(magnetUri);
        string infoHash = magnet.InfoHashes.V1OrV2.ToHex();

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_managers.ContainsKey(infoHash))
            {
                _logger.LogWarning("Torrent {Hash} already added.", infoHash);
                return _items[infoHash];
            }

            Directory.CreateDirectory(savePath);

            TorrentManager manager = await _engine!.AddAsync(magnet, savePath)
                .ConfigureAwait(false);

            string name = magnet.Name ?? infoHash[..8];
            TorrentItem item = BuildItem(manager, infoHash, name, savePath);
            item.Status = TorrentStatus.FetchingMetadata;

            RegisterManager(manager, item);

            TorrentAdded?.Invoke(this, item);
            _logger.LogInformation(
                "Magnet added (metadata pending): {Name} [{Hash}]", name, infoHash);
            return item;
        }
        finally
        {
            _lock.Release();
        }
    }

    // -----------------------------------------------------------------------
    // ITorrentService — Start / Pause / Remove
    // -----------------------------------------------------------------------

    public async Task StartAsync(string infoHash, CancellationToken cancellationToken = default)
    {
        TorrentManager manager = GetManager(infoHash);
        await manager.StartAsync().ConfigureAwait(false);
        _logger.LogInformation("Torrent started: {Hash}", infoHash);
    }

    public async Task PauseAsync(string infoHash, CancellationToken cancellationToken = default)
    {
        TorrentManager manager = GetManager(infoHash);
        await manager.PauseAsync().ConfigureAwait(false);
        _logger.LogInformation("Torrent paused: {Hash}", infoHash);
    }

    public async Task RemoveAsync(
        string infoHash,
        bool deleteFiles,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_managers.TryGetValue(infoHash, out TorrentManager? manager))
            {
                return;
            }

            // Stop the torrent first.
            if (manager.State != TorrentState.Stopped)
            {
                await manager.StopAsync().ConfigureAwait(false);
            }

            // MonoTorrent 3.0.2: use RemoveMode enum
            RemoveMode mode = deleteFiles
                ? RemoveMode.CacheDataAndDownloadedData
                : RemoveMode.KeepAllData;

            await _engine!.RemoveAsync(manager, mode).ConfigureAwait(false);

            UnregisterManager(manager, infoHash);

            // Remove our cached .torrent file (unless MonoTorrent already did it)
            string torrentFile = Path.Combine(_torrentFileFolder, infoHash + ".torrent");
            if (File.Exists(torrentFile))
            {
                File.Delete(torrentFile);
            }

            TorrentRemoved?.Invoke(this, infoHash);
            _logger.LogInformation(
                "Torrent removed: {Hash} (deleteFiles={Delete})", infoHash, deleteFiles);
        }
        finally
        {
            _lock.Release();
        }
    }

    // -----------------------------------------------------------------------
    // ITorrentService — Speed Limits
    // -----------------------------------------------------------------------

    /// <summary>
    /// Sets global download/upload rate limits via engine-level settings.
    /// MonoTorrent 3.0.2 uses <see cref="EngineSettingsBuilder.MaximumDownloadRate"/>
    /// and <see cref="EngineSettingsBuilder.MaximumUploadRate"/> at the engine level.
    /// Since <see cref="ClientEngine"/> settings are immutable, we store the limits
    /// and apply them per-torrent via <see cref="TorrentSettingsBuilder"/>.
    /// </summary>
    public Task ApplySpeedLimitsAsync(int maxDownloadBps, int maxUploadBps)
    {
        EnsureInitialized();

        // Apply per-torrent settings since engine settings are immutable in 3.0.2.
        foreach (KeyValuePair<string, TorrentManager> kv in _managers)
        {
            var newSettings = new TorrentSettingsBuilder
            {
                MaximumDownloadRate = maxDownloadBps,
                MaximumUploadRate = maxUploadBps,
            }.ToSettings();

            // Post the settings change to the MonoTorrent main loop thread-safely.
            _ = kv.Value.UpdateSettingsAsync(newSettings);
        }

        _logger.LogInformation(
            "Speed limits applied: ↓{Down} B/s  ↑{Up} B/s", maxDownloadBps, maxUploadBps);

        return Task.CompletedTask;
    }

    // -----------------------------------------------------------------------
    // ITorrentService — SaveState
    // -----------------------------------------------------------------------

    public async Task SaveStateAsync(CancellationToken cancellationToken = default)
    {
        if (_engine is null)
        {
            return;
        }

        try
        {
            // MonoTorrent's AutoSaveLoadFastResume writes fast-resume data on StopAsync.
            // Stop all active torrents so fast-resume is written to disk.
            await _engine.StopAllAsync().ConfigureAwait(false);
            _logger.LogInformation("Engine state saved (all torrents stopped).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving engine state.");
        }
    }

    // -----------------------------------------------------------------------
    // IAsyncDisposable
    // -----------------------------------------------------------------------

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _lock.Dispose();

        if (_engine is not null)
        {
            try
            {
                await _engine.StopAllAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during engine shutdown.");
            }

            _engine.Dispose();
        }

        _logger.LogInformation("TorrentService disposed.");
    }

    // -----------------------------------------------------------------------
    // Statistics polling (called periodically by DownloadsViewModel timer)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Copies live MonoTorrent statistics into the app-owned <see cref="TorrentItem"/> models.
    /// Called every ~500 ms from the UI thread via the ViewModel's DispatcherTimer.
    /// </summary>
    public void RefreshStatistics()
    {
        foreach (KeyValuePair<string, TorrentManager> kv in _managers)
        {
            TorrentManager manager = kv.Value;
            TorrentItem? item;

            lock (_items)
            {
                _items.TryGetValue(kv.Key, out item);
            }

            if (item is null)
            {
                continue;
            }

            item.Progress = manager.Progress / 100.0;
            item.DownloadSpeedBps = manager.Monitor.DownloadRate;
            item.UploadSpeedBps = manager.Monitor.UploadRate;
            // MonoTorrent 3.0.2: DataBytesReceived/DataBytesSent
            item.DownloadedBytes = manager.Monitor.DataBytesReceived;
            item.UploadedBytes = manager.Monitor.DataBytesSent;
            item.Peers = manager.Peers.Available;
            item.Seeds = manager.Peers.Seeds;
            item.Status = MapStatus(manager.State);

            if (manager.HasMetadata && item.TotalBytes == 0)
            {
                item.TotalBytes = manager.Torrent?.Size ?? 0;
                item.Name = manager.Torrent?.Name ?? item.Name;
            }

            if (item.DownloadSpeedBps > 0 && item.TotalBytes > item.DownloadedBytes)
            {
                long remaining = item.TotalBytes - item.DownloadedBytes;
                item.Eta = TimeSpan.FromSeconds(remaining / (double)item.DownloadSpeedBps);
            }
            else
            {
                item.Eta = null;
            }
        }
    }

    // -----------------------------------------------------------------------
    // Internal helpers
    // -----------------------------------------------------------------------

    private async Task RestoreStateAsync(CancellationToken cancellationToken)
    {
        // Re-add every .torrent file we have on disk.
        // MonoTorrent 3.0.2 with AutoSaveLoadFastResume=true will automatically
        // restore fast-resume data when each manager calls StartAsync.
        string[] torrentFiles = Directory.GetFiles(_torrentFileFolder, "*.torrent");
        _logger.LogInformation("Restoring {Count} persisted torrent(s).", torrentFiles.Length);

        foreach (string file in torrentFiles)
        {
            try
            {
                Torrent torrent = await Torrent.LoadAsync(file).ConfigureAwait(false);
                string infoHash = torrent.InfoHashes.V1OrV2.ToHex();

                if (_managers.ContainsKey(infoHash))
                {
                    continue;
                }

                string savePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads", "Torrents");

                TorrentManager manager = await _engine!.AddAsync(torrent, savePath)
                    .ConfigureAwait(false);

                TorrentItem item = BuildItem(manager, infoHash, torrent.Name, savePath);
                RegisterManager(manager, item);

                _logger.LogInformation(
                    "Restored torrent: {Name} [{Hash}]", torrent.Name, infoHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore torrent from {File}", file);
            }
        }
    }

    private TorrentItem BuildItem(
        TorrentManager manager,
        string infoHash,
        string name,
        string savePath)
    {
        var item = new TorrentItem
        {
            InfoHash = infoHash,
            Name = name,
            SavePath = savePath,
            Status = MapStatus(manager.State),
            Progress = manager.Progress / 100.0,
        };

        if (manager.HasMetadata && manager.Torrent is not null)
        {
            item.TotalBytes = manager.Torrent.Size;
            item.Files = [.. manager.Files
                .Select(f => new TorrentFileInfo
                {
                    Path = f.Path,
                    Length = f.Length,
                    Progress = f.BitField?.PercentComplete / 100.0 ?? 0,
                })];
        }

        lock (_items)
        {
            _items[infoHash] = item;
        }

        return item;
    }

    private void RegisterManager(TorrentManager manager, TorrentItem item)
    {
        _managers[item.InfoHash] = manager;
        manager.TorrentStateChanged += OnManagerStateChanged;
    }

    private void UnregisterManager(TorrentManager manager, string infoHash)
    {
        manager.TorrentStateChanged -= OnManagerStateChanged;
        _managers.Remove(infoHash);

        lock (_items)
        {
            _items.Remove(infoHash);
        }
    }

    private TorrentManager GetManager(string infoHash)
    {
        if (!_managers.TryGetValue(infoHash, out TorrentManager? manager))
        {
            throw new InvalidOperationException(
                $"Torrent {infoHash} is not managed by this service.");
        }

        return manager;
    }

    private void EnsureInitialized()
    {
        if (_engine is null)
        {
            throw new InvalidOperationException(
                "TorrentService.InitializeAsync must be called before any other method.");
        }
    }

    // -----------------------------------------------------------------------
    // MonoTorrent event handlers
    // -----------------------------------------------------------------------

    private void OnManagerStateChanged(object? sender, TorrentStateChangedEventArgs e)
    {
        if (sender is not TorrentManager manager)
        {
            return;
        }

        string infoHash = manager.InfoHashes.V1OrV2.ToHex();
        TorrentItem? item;

        lock (_items)
        {
            _items.TryGetValue(infoHash, out item);
        }

        if (item is null)
        {
            return;
        }

        item.Status = MapStatus(e.NewState);

        // Populate file list once metadata arrives for magnet links.
        if (manager.HasMetadata && item.TotalBytes == 0 && manager.Torrent is not null)
        {
            item.TotalBytes = manager.Torrent.Size;
            item.Name = manager.Torrent.Name;
            item.Files = [.. manager.Files
                .Select(f => new TorrentFileInfo
                {
                    Path = f.Path,
                    Length = f.Length,
                    Progress = f.BitField?.PercentComplete / 100.0 ?? 0,
                })];
        }

        _logger.LogInformation(
            "Torrent state changed: {Name} → {State}", item.Name, e.NewState);

        TorrentStateChanged?.Invoke(this, item);

        if (e.NewState == TorrentState.Seeding)
        {
            item.Status = TorrentStatus.Completed;
            TorrentCompleted?.Invoke(this, item);
            _logger.LogInformation("Torrent completed (seeding): {Name}", item.Name);
        }
    }

    // -----------------------------------------------------------------------
    // Status mapping
    // -----------------------------------------------------------------------

    private static TorrentStatus MapStatus(TorrentState state) => state switch
    {
        TorrentState.Stopped => TorrentStatus.Paused,
        TorrentState.Paused => TorrentStatus.Paused,
        TorrentState.Starting => TorrentStatus.Queued,
        TorrentState.Downloading => TorrentStatus.Downloading,
        TorrentState.Seeding => TorrentStatus.Seeding,
        TorrentState.Hashing => TorrentStatus.Checking,
        TorrentState.HashingPaused => TorrentStatus.Checking,
        TorrentState.FetchingHashes => TorrentStatus.Checking,
        TorrentState.Metadata => TorrentStatus.FetchingMetadata,
        TorrentState.Error => TorrentStatus.Error,
        _ => TorrentStatus.Queued,
    };
}

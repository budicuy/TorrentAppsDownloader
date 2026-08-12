using TorrentApp.Models;

namespace TorrentApp.Services;

/// <summary>
/// Abstraction over application-level settings persistence.
/// </summary>
public interface ISettingsService
{
    /// <summary>Returns the current settings. Never null.</summary>
    TorrentSettings Settings { get; }

    /// <summary>Loads settings from persistent storage. Should be called at startup.</summary>
    Task LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists the current settings to storage.</summary>
    Task SaveAsync(CancellationToken cancellationToken = default);
}

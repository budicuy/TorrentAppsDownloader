using System.Text.Json;
using Microsoft.Extensions.Logging;
using TorrentApp.Models;
using Windows.Storage;

namespace TorrentApp.Services;

/// <summary>
/// Persists <see cref="TorrentSettings"/> as JSON in the application's local data folder.
/// </summary>
internal sealed class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly ILogger<SettingsService> _logger;
    private readonly string _settingsFilePath;

    public TorrentSettings Settings { get; private set; } = new();

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
        string localFolder = GetLocalDataFolder();
        _settingsFilePath = Path.Combine(localFolder, "settings.json");
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

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                _logger.LogInformation("No settings file found; using defaults.");
                Settings = new TorrentSettings();
                return;
            }

            await using FileStream stream = File.OpenRead(_settingsFilePath);
            TorrentSettings? loaded = await JsonSerializer
                .DeserializeAsync<TorrentSettings>(stream, s_jsonOptions, cancellationToken)
                .ConfigureAwait(false);

            Settings = loaded ?? new TorrentSettings();
            _logger.LogInformation("Settings loaded from {Path}", _settingsFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings; using defaults.");
            Settings = new TorrentSettings();
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            string? dir = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await using FileStream stream = File.Create(_settingsFilePath);
            await JsonSerializer
                .SerializeAsync(stream, Settings, s_jsonOptions, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation("Settings saved to {Path}", _settingsFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings.");
        }
    }
}

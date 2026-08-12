using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TorrentApp.Services;

namespace TorrentApp.ViewModels;

/// <summary>
/// ViewModel for the Settings page.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly ITorrentService _torrentService;
    private readonly ILogger<SettingsViewModel> _logger;

    // -----------------------------------------------------------------------
    // Observable properties
    // -----------------------------------------------------------------------

    [ObservableProperty]
    public partial string DefaultSavePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SelectedTheme { get; set; } = "System";

    [ObservableProperty]
    public partial int MaxDownloadSpeedIndex { get; set; }

    [ObservableProperty]
    public partial int MaxUploadSpeedIndex { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    // Speed preset arrays (indices match combo box items)
    public static readonly int[] SpeedPresets = [0, 102_400, 512_000, 1_048_576, 5_242_880, 10_485_760];
    public static readonly string[] SpeedLabels = ["Unlimited", "100 KB/s", "500 KB/s", "1 MB/s", "5 MB/s", "10 MB/s"];
    public static readonly string[] ThemeOptions = ["System", "Light", "Dark"];

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);
    partial void OnStatusMessageChanged(string value) => OnPropertyChanged(nameof(HasStatusMessage));

    // -----------------------------------------------------------------------
    // Constructor
    // -----------------------------------------------------------------------

    public SettingsViewModel(
        ISettingsService settingsService,
        ITorrentService torrentService,
        ILogger<SettingsViewModel> logger)
    {
        _settingsService = settingsService;
        _torrentService = torrentService;
        _logger = logger;

        LoadFromSettings();
    }

    // -----------------------------------------------------------------------
    // Commands
    // -----------------------------------------------------------------------

    [RelayCommand]
    public async Task SaveSettingsAsync()
    {
        try
        {
            _settingsService.Settings.DefaultSavePath = DefaultSavePath;
            _settingsService.Settings.Theme = SelectedTheme;
            _settingsService.Settings.MaxDownloadSpeedBps =
                MaxDownloadSpeedIndex < SpeedPresets.Length
                    ? SpeedPresets[MaxDownloadSpeedIndex]
                    : 0;
            _settingsService.Settings.MaxUploadSpeedBps =
                MaxUploadSpeedIndex < SpeedPresets.Length
                    ? SpeedPresets[MaxUploadSpeedIndex]
                    : 0;

            await _settingsService.SaveAsync().ConfigureAwait(true);
            await _torrentService.ApplySpeedLimitsAsync(
                _settingsService.Settings.MaxDownloadSpeedBps,
                _settingsService.Settings.MaxUploadSpeedBps).ConfigureAwait(true);

            StatusMessage = "Settings saved.";
            _logger.LogInformation("Settings saved by user.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings.");
            StatusMessage = "Could not save settings. Please try again.";
        }
    }

    [RelayCommand]
    public static void BrowseSavePath()
    {
        // Browse is implemented in code-behind (requires Window handle for folder picker).
        // This command signals the view to open the picker.
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private void LoadFromSettings()
    {
        DefaultSavePath = _settingsService.Settings.DefaultSavePath;
        SelectedTheme = _settingsService.Settings.Theme;

        int dlIndex = Array.IndexOf(SpeedPresets, _settingsService.Settings.MaxDownloadSpeedBps);
        MaxDownloadSpeedIndex = dlIndex >= 0 ? dlIndex : 0;

        int ulIndex = Array.IndexOf(SpeedPresets, _settingsService.Settings.MaxUploadSpeedBps);
        MaxUploadSpeedIndex = ulIndex >= 0 ? ulIndex : 0;
    }
}

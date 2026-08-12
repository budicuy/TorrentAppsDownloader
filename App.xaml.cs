using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using TorrentApp.Services;
using TorrentApp.ViewModels;

namespace TorrentApp;

/// <summary>
/// Application entry point. Configures the DI container, starts the engine, and handles
/// clean shutdown including engine state persistence and resource disposal.
/// </summary>
public partial class App : Application
{
    /// <summary>The application's DI service provider.</summary>
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>The main application window.</summary>
    public static Window Window { get; private set; } = null!;

    /// <summary>UI-thread dispatcher queue. Fully qualified to avoid CS0104 ambiguity.</summary>
    public static Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; private set; } = null!;

    /// <summary>Native window handle (HWND) for WinRT interop (pickers, etc.).</summary>
    public static nint WindowHandle => WinRT.Interop.WindowNative.GetWindowHandle(Window);

    private readonly CancellationTokenSource _shutdownCts = new();

    public App()
    {
        Services = ConfigureServices();
        InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        // Initialize settings first so the engine can use them.
        ISettingsService settings = Services.GetRequiredService<ISettingsService>();
        await settings.LoadAsync(_shutdownCts.Token).ConfigureAwait(true);

        // Apply theme from settings.
        ApplyTheme(settings.Settings.Theme);

        // Start the BitTorrent engine and restore persisted torrents.
        ITorrentService torrentService = Services.GetRequiredService<ITorrentService>();
        await torrentService.InitializeAsync(_shutdownCts.Token).ConfigureAwait(true);

        // Apply speed limits from settings.
        await torrentService.ApplySpeedLimitsAsync(
            settings.Settings.MaxDownloadSpeedBps,
            settings.Settings.MaxUploadSpeedBps).ConfigureAwait(true);

        Window = new MainWindow();
        Window.Closed += OnWindowClosed;
        Window.Activate();
    }

    private async void OnWindowClosed(object sender, WindowEventArgs args)
    {
        // Signal shutdown to all background operations.
        await _shutdownCts.CancelAsync().ConfigureAwait(false);

        // Persist state before we exit.
        ITorrentService torrentService = Services.GetRequiredService<ITorrentService>();
        await torrentService.SaveStateAsync(CancellationToken.None).ConfigureAwait(false);

        ISettingsService settingsService = Services.GetRequiredService<ISettingsService>();
        await settingsService.SaveAsync(CancellationToken.None).ConfigureAwait(false);

        // Dispose the engine (stops MonoTorrent, releases sockets/files).
        await torrentService.DisposeAsync().ConfigureAwait(false);

        _shutdownCts.Dispose();
    }

    // -----------------------------------------------------------------------
    // DI configuration
    // -----------------------------------------------------------------------

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddConsole();
        });

        // Services (singletons because the engine must have a single lifetime)
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ITorrentService, TorrentService>();

        // ViewModels (transient — new instance per navigation)
        services.AddTransient<DownloadsViewModel>();
        services.AddTransient<SettingsViewModel>();

        return services.BuildServiceProvider();
    }

    // -----------------------------------------------------------------------
    // Theme helpers
    // -----------------------------------------------------------------------

    public static void ApplyTheme(string theme)
    {
        if (Window?.Content is FrameworkElement root)
        {
            root.RequestedTheme = theme switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default,
            };
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TorrentApp.ViewModels;
using Windows.Storage.Pickers;

namespace TorrentApp.Views;

/// <summary>
/// Code-behind for the Downloads page. UI-specific interactions (file picker, dialogs)
/// live here; business logic lives in <see cref="DownloadsViewModel"/>.
/// </summary>
public sealed partial class DownloadsPage : Page
{
    public DownloadsViewModel ViewModel { get; }

    public DownloadsPage()
    {
        ViewModel = App.Services.GetRequiredService<DownloadsViewModel>();
        InitializeComponent();
    }

    // -----------------------------------------------------------------------
    // Add .torrent file
    // -----------------------------------------------------------------------

    private async void AddFileButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker
        {
            ViewMode = PickerViewMode.List,
            SuggestedStartLocation = PickerLocationId.Downloads,
        };
        picker.FileTypeFilter.Add(".torrent");

        // WinRT interop: associate picker with the window HWND.
        WinRT.Interop.InitializeWithWindow.Initialize(picker, App.WindowHandle);

        Windows.Storage.StorageFile? file = await picker.PickSingleFileAsync();
        if (file is null)
        {
            return;
        }

        await ViewModel.AddTorrentFileCommand.ExecuteAsync(file.Path);
    }

    // -----------------------------------------------------------------------
    // Add Magnet Link
    // -----------------------------------------------------------------------

    private async void AddMagnetButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Add Magnet Link",
            PrimaryButtonText = "Add",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot,
        };

        var textBox = new TextBox
        {
            PlaceholderText = "magnet:?xt=urn:btih:...",
            Width = 420,
            AcceptsReturn = false,
        };

        dialog.Content = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Paste a magnet link below:" },
                textBox,
            },
        };

        ContentDialogResult result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        string uri = textBox.Text.Trim();
        if (!uri.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
        {
            ShowError("Invalid magnet link. It must start with \"magnet:\".");
            return;
        }

        await ViewModel.AddMagnetLinkCommand.ExecuteAsync(uri);
    }

    // -----------------------------------------------------------------------
    // Torrent row buttons
    // -----------------------------------------------------------------------

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TorrentItemViewModel vm })
        {
            await ViewModel.StartTorrentCommand.ExecuteAsync(vm);
        }
    }

    private async void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TorrentItemViewModel vm })
        {
            await ViewModel.PauseTorrentCommand.ExecuteAsync(vm);
        }
    }

    private void OpenFolderItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: TorrentItemViewModel vm })
        {
            ViewModel.OpenFolderCommand.Execute(vm);
        }
    }

    private async void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: TorrentItemViewModel vm })
        {
            await ConfirmAndRemoveAsync(vm, deleteFiles: false);
        }
    }

    private async void RemoveWithFilesItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: TorrentItemViewModel vm })
        {
            await ConfirmAndRemoveAsync(vm, deleteFiles: true);
        }
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private async Task ConfirmAndRemoveAsync(TorrentItemViewModel vm, bool deleteFiles)
    {
        string message = deleteFiles
            ? $"Remove \"{vm.Name}\" and delete all downloaded files? This cannot be undone."
            : $"Remove \"{vm.Name}\" from the list? Downloaded files will not be deleted.";

        var dialog = new ContentDialog
        {
            Title = "Confirm removal",
            Content = message,
            PrimaryButtonText = deleteFiles ? "Remove + Delete Files" : "Remove",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot,
        };

        ContentDialogResult result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.RemoveTorrentCommand.ExecuteAsync((vm, deleteFiles));
        }
    }

    private void ShowError(string message)
    {
        StatusInfoBar.Severity = InfoBarSeverity.Error;
        StatusInfoBar.Message = message;
        StatusInfoBar.IsOpen = true;
    }
}

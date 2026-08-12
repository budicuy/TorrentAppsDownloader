using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TorrentApp.Models;
using TorrentApp.Services;
using TorrentApp.ViewModels;

namespace TorrentApp.Views;

/// <summary>
/// Shows all completed torrents, filtered from the main engine state.
/// </summary>
public sealed partial class CompletedPage : Page
{
    public ObservableCollection<TorrentItemViewModel> CompletedTorrents { get; } = [];
    public bool IsEmpty => CompletedTorrents.Count == 0;

    public CompletedPage()
    {
        InitializeComponent();

        // Build completed list from current engine state.
        ITorrentService torrentService = App.Services.GetRequiredService<ITorrentService>();
        foreach (TorrentItem item in torrentService.GetAllTorrents())
        {
            if (item.Status is TorrentStatus.Completed or TorrentStatus.Seeding)
            {
                CompletedTorrents.Add(new TorrentItemViewModel(item));
            }
        }

        // Subscribe to completed event.
        torrentService.TorrentCompleted += OnTorrentCompleted;
        Unloaded += (_, _) => torrentService.TorrentCompleted -= OnTorrentCompleted;
    }

    private void OnTorrentCompleted(object? sender, TorrentItem item)
    {
        App.DispatcherQueue.TryEnqueue(() =>
        {
            if (CompletedTorrents.Any(vm => vm.InfoHash == item.InfoHash))
            {
                return;
            }

            CompletedTorrents.Add(new TorrentItemViewModel(item));
            OnPropertyChanged(nameof(IsEmpty));
        });
    }

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TorrentItemViewModel vm }
            && !string.IsNullOrWhiteSpace(vm.SavePath)
            && Directory.Exists(vm.SavePath))
        {
            System.Diagnostics.Process.Start("explorer.exe", vm.SavePath);
        }
    }

    // Minimal INotifyPropertyChanged for IsEmpty binding
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
}

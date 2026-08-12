using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TorrentApp.Views;

namespace TorrentApp;

/// <summary>
/// Main application window. Hosts a <see cref="NavigationView"/> with Downloads,
/// Completed, and Settings pages. Navigation state lives here; business logic lives
/// in ViewModels and Services.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // WinUI 1.5+ TitleBar control handles ExtendsContentIntoTitleBar automatically.
        // We only need to call SetTitleBar so drag regions work correctly.
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        // Select Downloads as the default page on first load.
        NavView.SelectedItem = NavDownloads;
        ContentFrame.Navigate(typeof(DownloadsPage));
    }

    private void NavView_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        if (args.SelectedItem is NavigationViewItem item)
        {
            Type? pageType = (item.Tag as string) switch
            {
                "downloads" => typeof(DownloadsPage),
                "completed" => typeof(CompletedPage),
                _ => null,
            };

            if (pageType is not null && ContentFrame.CurrentSourcePageType != pageType)
            {
                ContentFrame.Navigate(pageType);
            }
        }
    }
}

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using OrganizadorDescargas.Pages;
using OrganizadorDescargas.Services;
using System;
using Windows.UI;

namespace OrganizadorDescargas;

public sealed partial class MainWindow : Window
{
    public static ConfigService Config { get; } = new();
    public static OrganizerService Organizer { get; } = new(Config);

    private const string Version = "3.0.0";
    private string? _updateUrl;

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.SetIcon("Assets/AppIcon.ico");
        AppWindow.Title = $"Organizador de PC  v{Version}";

        Organizer.StatusChanged += OnStatusChanged;
        Organizer.LogEmitted += OnLogEmitted;
        Organizer.FileMoved += OnFileMoved;

        // Apply saved theme BEFORE navigating so pages render with correct theme
        ApplyTheme(Config.Get().Theme);

        NavView.SelectedItem = NavView.MenuItems[0];
        NavFrame.Navigate(typeof(DashboardPage));

        if (Config.Get().AutoStartMonitoring)
            DispatcherQueue.TryEnqueue(
                Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                Organizer.Start);

        _ = CheckUpdatesAsync();
    }

    // ── navigation ────────────────────────────────────────────────────────────

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs e)
    {
        if (e.SelectedItem is not NavigationViewItem item) return;
        Type? page = item.Tag?.ToString() switch
        {
            "dashboard"  => typeof(DashboardPage),
            "folders"    => typeof(FolderOrganizerPage),
            "desktop"    => typeof(DesktopOrganizerPage),
            "rules"      => typeof(RulesPage),
            "exclusions" => typeof(ExclusionsPage),
            "logs"       => typeof(LogsPage),
            "settings"   => typeof(SettingsPage),
            _ => null
        };
        if (page != null && NavFrame.CurrentSourcePageType != page)
            NavFrame.Navigate(page);
    }

    // ── organizer events ──────────────────────────────────────────────────────

    private void OnStatusChanged(bool running)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var color = running
                ? Color.FromArgb(255, 16, 185, 129)
                : Color.FromArgb(255, 150, 150, 150);
            StatusEllipse.Fill = new SolidColorBrush(color);
            StatusText.Foreground = new SolidColorBrush(color);
            StatusText.Text = running ? "Activo" : "Detenido";

            if (NavFrame.Content is DashboardPage dash)
                dash.UpdateStatus(running);
        });
    }

    private void OnLogEmitted(LogEntry entry)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (NavFrame.Content is LogsPage logs)
                logs.AppendLog(entry);
            if (NavFrame.Content is DashboardPage dash)
                dash.AppendActivity(entry);
        });
    }

    private void OnFileMoved(FileMoved data)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (NavFrame.Content is DashboardPage dash)
                dash.OnFileMoved(data);
        });
    }

    // ── updates ───────────────────────────────────────────────────────────────

    private async System.Threading.Tasks.Task CheckUpdatesAsync()
    {
        await System.Threading.Tasks.Task.Delay(2000);
        var info = await UpdaterService.CheckAsync(Version, Config.Get().UpdateCheckUrl);
        if (info == null) return;
        _updateUrl = info.Url;
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateText.Text = $"  Nueva versión {info.Version} disponible";
            UpdateBanner.Visibility = Visibility.Visible;
        });
    }

    public void ApplyTheme(string theme)
    {
        var requested = theme switch
        {
            "light"   => ElementTheme.Light,
            "default" => ElementTheme.Default,
            _ => ElementTheme.Dark,
        };
        if (Content is FrameworkElement root)
            root.RequestedTheme = requested;
    }

    private void DownloadBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_updateUrl != null)
            _ = Windows.System.Launcher.LaunchUriAsync(new Uri(_updateUrl));
    }

    private void DismissBanner_Click(object sender, RoutedEventArgs e)
    {
        UpdateBanner.Visibility = Visibility.Collapsed;
    }
}

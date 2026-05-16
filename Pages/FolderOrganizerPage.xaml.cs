using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using OrganizadorDescargas.Models;
using OrganizadorDescargas.Services;
using Windows.Storage.Pickers;
using Windows.UI;

namespace OrganizadorDescargas.Pages;

public sealed partial class FolderOrganizerPage : Page
{
    private ScanResult? _lastScan;
    private readonly FolderOrganizerService _svc = new(MainWindow.Config);

    public FolderOrganizerPage()
    {
        InitializeComponent();
    }

    private async void BrowseBtn_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        picker.SuggestedStartLocation = PickerLocationId.Desktop;
        picker.FileTypeFilter.Add("*");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder != null)
            FolderBox.Text = folder.Path;
    }

    private async void ScanBtn_Click(object sender, RoutedEventArgs e)
    {
        var path = FolderBox.Text;
        if (string.IsNullOrWhiteSpace(path)) return;

        ScanBtn.IsEnabled = false;
        ScanBtn.Content = "Escaneando…";

        _lastScan = await _svc.ScanAsync(path);

        TotalFilesLabel.Text = _lastScan.TotalFiles.ToString();
        WillMoveLabel.Text = _lastScan.FilesWithCategory.ToString();
        NoRuleLabel.Text = _lastScan.FilesWithoutCategory.ToString();
        StatsBar.Visibility = Visibility.Visible;

        OrganizerPageHelper.BuildPreview(PreviewPanel, _lastScan);
        PreviewSection.Visibility = Visibility.Visible;

        OrganizeBtn.Content = "Organizar ahora";
        OrganizeBtn.Background = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129));
        OrganizeBtn.IsEnabled = true;
        OrganizeBtn.Visibility = _lastScan.FilesWithCategory > 0 ? Visibility.Visible : Visibility.Collapsed;

        ScanBtn.IsEnabled = true;
        ScanBtn.Content = "Escanear";
    }

    private async void OrganizeBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_lastScan == null) return;
        OrganizeBtn.IsEnabled = false;
        OrganizeBtn.Content = "Organizando…";

        var moved = await _svc.ApplyAsync(_lastScan);

        _lastScan = await _svc.ScanAsync(_lastScan.FolderPath);
        TotalFilesLabel.Text = _lastScan.TotalFiles.ToString();
        WillMoveLabel.Text = _lastScan.FilesWithCategory.ToString();
        NoRuleLabel.Text = _lastScan.FilesWithoutCategory.ToString();
        OrganizerPageHelper.BuildPreview(PreviewPanel, _lastScan);

        OrganizeBtn.Content = $"✓  {moved} archivo{(moved == 1 ? "" : "s")} organizado{(moved == 1 ? "" : "s")}";
        OrganizeBtn.Background = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235));

        if (_lastScan.FilesWithCategory == 0)
            OrganizeBtn.Visibility = Visibility.Collapsed;
    }
}

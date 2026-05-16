using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using OrganizadorDescargas.Models;
using OrganizadorDescargas.Services;
using System;
using System.Collections.Generic;
using Windows.UI;

namespace OrganizadorDescargas.Pages;

public sealed partial class DesktopOrganizerPage : Page
{
    private static readonly string DesktopPath =
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

    private static readonly Dictionary<string, string> ExtraRules = new()
    {
        { ".lnk", "Accesos Directos" },
        { ".url", "Accesos Directos" },
    };

    private ScanResult? _lastScan;
    private readonly FolderOrganizerService _svc = new(MainWindow.Config);

    public DesktopOrganizerPage()
    {
        InitializeComponent();
    }

    private async void ScanBtn_Click(object sender, RoutedEventArgs e)
    {
        ScanBtn.IsEnabled = false;
        ScanBtn.Content = "Escaneando…";

        _lastScan = await _svc.ScanAsync(DesktopPath, ExtraRules);

        TotalFilesLabel.Text = _lastScan.TotalFiles.ToString();
        WillMoveLabel.Text = _lastScan.FilesWithCategory.ToString();
        NoRuleLabel.Text = _lastScan.FilesWithoutCategory.ToString();
        StatsBar.Visibility = Visibility.Visible;

        OrganizerPageHelper.BuildPreview(PreviewPanel, _lastScan);
        PreviewSection.Visibility = Visibility.Visible;

        OrganizeBtn.Content = "Organizar escritorio";
        OrganizeBtn.Background = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129));
        OrganizeBtn.IsEnabled = true;
        OrganizeBtn.Visibility = _lastScan.FilesWithCategory > 0 ? Visibility.Visible : Visibility.Collapsed;

        ScanBtn.IsEnabled = true;
        ScanBtn.Content = "Escanear escritorio";
    }

    private async void OrganizeBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_lastScan == null) return;

        var dlg = new ContentDialog
        {
            Title = "Organizar escritorio",
            Content = $"Se moverán {_lastScan.FilesWithCategory} archivo(s) a subcarpetas del escritorio.\n\nPuedes deshacerlo moviendo los archivos de vuelta manualmente.\n\n¿Continuar?",
            PrimaryButtonText = "Organizar",
            CloseButtonText = "Cancelar",
            XamlRoot = XamlRoot,
        };
        if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

        OrganizeBtn.IsEnabled = false;
        OrganizeBtn.Content = "Organizando…";

        var moved = await _svc.ApplyAsync(_lastScan);

        _lastScan = await _svc.ScanAsync(DesktopPath, ExtraRules);
        TotalFilesLabel.Text = _lastScan.TotalFiles.ToString();
        WillMoveLabel.Text = _lastScan.FilesWithCategory.ToString();
        NoRuleLabel.Text = _lastScan.FilesWithoutCategory.ToString();
        OrganizerPageHelper.BuildPreview(PreviewPanel, _lastScan);

        OrganizeBtn.Content = $"✓  {moved} archivo{(moved == 1 ? "" : "s")} organizado{(moved == 1 ? "" : "s")}";
        OrganizeBtn.Background = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235));
        OrganizeBtn.IsEnabled = false;

        if (_lastScan.FilesWithCategory == 0)
            OrganizeBtn.Visibility = Visibility.Collapsed;
    }
}

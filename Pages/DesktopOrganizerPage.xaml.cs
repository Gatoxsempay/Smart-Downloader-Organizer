using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OrganizadorDescargas.Models;
using System;
using System.Collections.Generic;

namespace OrganizadorDescargas.Pages;

public sealed partial class DesktopOrganizerPage : FolderOrganizerPage
{
    private static readonly string DesktopPath =
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

    private static readonly Dictionary<string, string> DesktopExtraRules = new()
    {
        { ".lnk", "Accesos Directos" },
        { ".url", "Accesos Directos" },
    };

    public DesktopOrganizerPage()
    {
        InitializeComponent();
        FolderBox.Text = DesktopPath;
    }

    protected override async void ScanBtn_Click(object sender, RoutedEventArgs e)
    {
        await RunScan(DesktopPath, DesktopExtraRules);
    }

    protected override async void OrganizeBtn_Click(object sender, RoutedEventArgs e)
    {
        if (LastScan == null) return;

        var dlg = new ContentDialog
        {
            Title = "Organizar escritorio",
            Content = $"Se moverán {LastScan.FilesWithCategory} archivo(s) a subcarpetas del escritorio.\n\nPuedes deshacer esto manualmente moviendo los archivos de vuelta.\n\n¿Continuar?",
            PrimaryButtonText = "Organizar",
            CloseButtonText = "Cancelar",
            XamlRoot = XamlRoot,
        };
        if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

        OrganizeBtn.IsEnabled = false;
        OrganizeBtn.Content = "Organizando…";

        var moved = await OrganizerSvc.ApplyAsync(LastScan);

        LastScan = await OrganizerSvc.ScanAsync(DesktopPath, DesktopExtraRules);
        TotalFilesLabel.Text = LastScan.TotalFiles.ToString();
        WillMoveLabel.Text = LastScan.FilesWithCategory.ToString();
        NoRuleLabel.Text = LastScan.FilesWithoutCategory.ToString();
        BuildPreview(LastScan);

        OrganizeBtn.Content = $"✓  {moved} archivo{(moved == 1 ? "" : "s")} organizado{(moved == 1 ? "" : "s")}";
        OrganizeBtn.IsEnabled = false;

        if (LastScan.FilesWithCategory == 0)
            OrganizeBtn.Visibility = Visibility.Collapsed;
    }
}

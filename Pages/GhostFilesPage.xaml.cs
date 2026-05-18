using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using OrganizadorDescargas.Models;
using OrganizadorDescargas.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;

namespace OrganizadorDescargas.Pages;

public sealed partial class GhostFilesPage : Page
{
    private readonly GhostFilesService     _svc  = new();
    private CancellationTokenSource?       _cts;
    private List<GhostFile>                _results = new();

    public GhostFilesPage()
    {
        InitializeComponent();
        GhostList.SelectionChanged += (_, _) => UpdateCountLabel();
        Loaded += async (_, _) => await StartScanAsync();
    }

    // ── escanear ──────────────────────────────────────────────────────────────

    private async void ScanBtn_Click(object sender, RoutedEventArgs e) => await StartScanAsync();

    private async Task StartScanAsync()
    {
        var folders = GetDefaultFolders();
        if (folders.Length == 0) { await ShowMsg("No se encontraron carpetas para analizar."); return; }

        int months    = int.Parse((MonthsCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "3");
        long minMb    = long.Parse((SizeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "100");
        long minBytes = minMb * 1_048_576;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        ScanBtn.IsEnabled        = false;
        ScanBtn.Content          = "Analizando…";
        StatusBar.Visibility     = Visibility.Visible;
        StatusText.Text          = "Iniciando…";
        ResultsBorder.Visibility = Visibility.Collapsed;
        ActionBar.Visibility     = Visibility.Collapsed;
        GhostList.ItemsSource    = null;

        var progress = new Progress<string>(msg => StatusText.Text = msg);

        List<GhostFile>? results = null;
        try   { results = await _svc.FindAsync(folders, minBytes, months, progress, ct); }
        catch (OperationCanceledException) { }
        catch (Exception ex) { await ShowMsg(ex.Message); }

        ScanBtn.IsEnabled    = true;
        ScanBtn.Content      = "Analizar";
        StatusBar.Visibility = Visibility.Collapsed;

        if (results == null) return;
        _results = results;
        PopulateList(results, months, minMb);
    }

    private static string[] GetDefaultFolders()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var candidates = new[]
        {
            Path.Combine(profile, "Downloads"),
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
        };
        return candidates.Where(Directory.Exists).ToArray();
    }

    // ── resultados ────────────────────────────────────────────────────────────

    private void PopulateList(List<GhostFile> results, int months, long minMb)
    {
        if (results.Count == 0)
        {
            GhostList.ItemsSource = null;
            ResultsBorder.Visibility = Visibility.Collapsed;
            ActionBar.Visibility     = Visibility.Collapsed;
            _ = ShowMsg($"No se encontraron archivos mayores a {minMb} MB sin acceso en los últimos {months} meses.");
            return;
        }

        GhostList.ItemsSource = results.Select(f => MakeRow(f)).ToList();
        ResultsBorder.Visibility = Visibility.Visible;
        ActionBar.Visibility     = Visibility.Visible;
        UpdateCountLabel();
    }

    private static Grid MakeRow(GhostFile f)
    {
        var row = new Grid { Tag = f, ColumnSpacing = 8 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star), MinWidth = 160 });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });

        row.Children.Add(new FontIcon
        {
            Glyph = "",   // Document icon
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
        });
        Grid.SetColumn((FrameworkElement)row.Children[0], 0);

        var nameBlock = new TextBlock
        {
            Text = f.Name, FontSize = 12,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
        };
        ToolTipService.SetToolTip(nameBlock, f.FullPath);
        row.Children.Add(nameBlock);
        Grid.SetColumn(nameBlock, 1);

        row.Children.Add(new TextBlock
        {
            Text = f.SizeDisplay, FontSize = 12, FontWeight = FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 248, 113, 113)),
            VerticalAlignment = VerticalAlignment.Center,
        });
        Grid.SetColumn((FrameworkElement)row.Children[2], 2);

        row.Children.Add(new TextBlock
        {
            Text = f.LastAccessDisplay, FontSize = 11,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            VerticalAlignment = VerticalAlignment.Center,
        });
        Grid.SetColumn((FrameworkElement)row.Children[3], 3);

        row.Children.Add(new TextBlock
        {
            Text = f.ModifiedDisplay, FontSize = 11,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            VerticalAlignment = VerticalAlignment.Center,
        });
        Grid.SetColumn((FrameworkElement)row.Children[4], 4);

        return row;
    }

    private void UpdateCountLabel()
    {
        int sel   = GhostList.SelectedItems?.Count ?? 0;
        int total = _results.Count;
        long selBytes = GhostList.SelectedItems?
            .OfType<Grid>()
            .Select(g => g.Tag as GhostFile)
            .Where(f => f != null)
            .Sum(f => f!.SizeBytes) ?? 0;

        CountLabel.Text = sel > 0
            ? $"{total} archivos encontrados · {sel} seleccionados ({FormatSize(selBytes)})"
            : $"{total} archivos encontrados · {FormatSize(_results.Sum(f => f.SizeBytes))} en total";
    }

    // ── acciones ──────────────────────────────────────────────────────────────

    private void OpenFolderBtn_Click(object sender, RoutedEventArgs e)
    {
        var selected = GhostList.SelectedItems?.OfType<Grid>()
            .Select(g => g.Tag as GhostFile).FirstOrDefault(f => f != null);

        if (selected != null)
        {
            string? dir = Path.GetDirectoryName(selected.FullPath);
            if (dir != null)
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{selected.FullPath}\"")
                    { UseShellExecute = true });
        }
    }

    private async void DeleteBtn_Click(object sender, RoutedEventArgs e)
    {
        var toDelete = GhostList.SelectedItems?
            .OfType<Grid>()
            .Select(g => g.Tag as GhostFile)
            .Where(f => f != null)
            .Select(f => f!)
            .ToList() ?? new();

        if (toDelete.Count == 0) { await ShowMsg("Selecciona uno o más archivos de la lista."); return; }

        long totalBytes = toDelete.Sum(f => f.SizeBytes);
        var dlg = new ContentDialog
        {
            Title             = "Confirmar eliminación",
            Content           = $"Se eliminarán {toDelete.Count} archivo(s) ({FormatSize(totalBytes)}) permanentemente. ¿Continuar?",
            PrimaryButtonText = "Eliminar",
            CloseButtonText   = "Cancelar",
            XamlRoot          = XamlRoot,
        };
        if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

        int deleted = 0, failed = 0;
        foreach (var f in toDelete)
        {
            try { File.Delete(f.FullPath); _results.Remove(f); deleted++; }
            catch { failed++; }
        }

        await ShowMsg($"{deleted} archivo(s) eliminado(s)" + (failed > 0 ? $", {failed} no se pudieron eliminar." : "."));

        if (_results.Count == 0)
        {
            ResultsBorder.Visibility = Visibility.Collapsed;
            ActionBar.Visibility     = Visibility.Collapsed;
        }
        else
        {
            GhostList.ItemsSource = _results.Select(f => MakeRow(f)).ToList();
            UpdateCountLabel();
        }
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F1} GB",
        >= 1_048_576     => $"{bytes / 1_048_576.0:F1} MB",
        >= 1_024         => $"{bytes / 1_024.0:F0} KB",
        _                => $"{bytes} B",
    };

    private async Task ShowMsg(string msg)
    {
        var dlg = new ContentDialog
        {
            Title = "Información", Content = msg,
            CloseButtonText = "OK", XamlRoot = XamlRoot,
        };
        await dlg.ShowAsync();
    }
}

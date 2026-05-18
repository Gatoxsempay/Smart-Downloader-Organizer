using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using OrganizadorDescargas.Models;
using OrganizadorDescargas.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;

namespace OrganizadorDescargas.Pages;

public sealed partial class DuplicatesPage : Page
{
    private readonly DuplicateFinderService _svc     = new();
    private CancellationTokenSource?        _cts;
    private readonly Dictionary<CheckBox, string> _checkMap = new();

    public DuplicatesPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await StartScanAsync();
    }

    // ── escanear ──────────────────────────────────────────────────────────────

    private async void ScanBtn_Click(object sender, RoutedEventArgs e) => await StartScanAsync();

    private async Task StartScanAsync()
    {
        var folders = GetDefaultFolders();
        if (folders.Length == 0) { await ShowMsg("No se encontraron carpetas para analizar."); return; }

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        ScanBtn.IsEnabled    = false;
        ScanBtn.Content      = "Buscando…";
        StatusBar.Visibility = Visibility.Visible;
        StatusText.Text      = "Iniciando…";
        ResultsList.Children.Clear();
        ActionBar.Visibility = Visibility.Collapsed;
        _checkMap.Clear();

        var progress = new Progress<string>(msg => StatusText.Text = msg);

        List<DuplicateGroup>? groups = null;
        try   { groups = await _svc.FindAsync(folders, progress, ct); }
        catch (OperationCanceledException) { }
        catch (Exception ex) { await ShowMsg(ex.Message); }

        ScanBtn.IsEnabled    = true;
        ScanBtn.Content      = "Volver a escanear";
        StatusBar.Visibility = Visibility.Collapsed;

        if (groups == null) return;
        BuildResults(groups);
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

    private void BuildResults(List<DuplicateGroup> groups)
    {
        ResultsList.Children.Clear();
        _checkMap.Clear();

        if (groups.Count == 0)
        {
            ResultsList.Children.Add(new TextBlock
            {
                Text = "No se encontraron duplicados.",
                FontSize = 13, Margin = new Thickness(0, 8, 0, 0),
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });
            return;
        }

        long totalWasted = groups.Sum(g => g.FileSize * (g.Files.Count - 1));
        ResultsList.Children.Add(MakeSummaryBanner(groups.Count, totalWasted));

        foreach (var g in groups)
            ResultsList.Children.Add(MakeGroupCard(g));

        ActionBar.Visibility = Visibility.Visible;
    }

    private static Border MakeSummaryBanner(int groupCount, long wastedBytes)
    {
        string wasted = FormatSize(wastedBytes);
        var banner = new Border
        {
            Background      = new SolidColorBrush(Color.FromArgb(30, 220, 38, 38)),
            BorderBrush     = new SolidColorBrush(Color.FromArgb(80, 220, 38, 38)),
            BorderThickness = new Thickness(1),
            CornerRadius    = new CornerRadius(10),
            Padding         = new Thickness(16, 12, 16, 12),
        };
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        sp.Children.Add(new FontIcon
        {
            Glyph = "",
            FontSize = 16,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 248, 113, 113)),
            VerticalAlignment = VerticalAlignment.Center,
        });
        var tb = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        tb.Inlines.Add(new Run { Text = $"{groupCount} grupos duplicados · ", FontWeight = FontWeights.SemiBold });
        tb.Inlines.Add(new Run { Text = $"{wasted} recuperables" });
        sp.Children.Add(tb);
        banner.Child = sp;
        return banner;
    }

    private Border MakeGroupCard(DuplicateGroup group)
    {
        var card = new Border
        {
            Background   = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            CornerRadius = new CornerRadius(10),
            Padding      = new Thickness(16, 12, 16, 12),
        };
        var sp = new StackPanel { Spacing = 6 };

        // Encabezado del grupo
        var hdr = new TextBlock { FontSize = 12, FontWeight = FontWeights.SemiBold };
        hdr.Inlines.Add(new Run { Text = $"{group.Files.Count} archivos · {group.SizeDisplay} c/u · " });
        hdr.Inlines.Add(new Run { Text = group.WastedDisplay, Foreground = new SolidColorBrush(Color.FromArgb(255, 248, 113, 113)) });
        sp.Children.Add(hdr);
        sp.Children.Add(new Border { Height = 1, Background = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"] });

        bool first = true;
        foreach (var filePath in group.Files)
        {
            var row = new Grid { ColumnSpacing = 8 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var cb = new CheckBox { VerticalAlignment = VerticalAlignment.Center, IsEnabled = !first };
            _checkMap[cb] = filePath;
            row.Children.Add(cb);
            Grid.SetColumn(cb, 0);

            row.Children.Add(new TextBlock
            {
                Text = filePath, FontSize = 11,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = first ? 0.7 : 1.0,
            });
            Grid.SetColumn((FrameworkElement)row.Children[1], 1);

            bool isFst = first;
            var chip = new Border
            {
                CornerRadius = new CornerRadius(6),
                Background   = new SolidColorBrush(isFst ? Color.FromArgb(25, 16, 185, 129) : Color.FromArgb(25, 220, 38, 38)),
                Padding      = new Thickness(6, 2, 6, 2),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = isFst ? "Original" : "Duplicado",
                    FontSize   = 10,
                    Foreground = new SolidColorBrush(isFst
                        ? Color.FromArgb(255, 52, 211, 153)
                        : Color.FromArgb(255, 248, 113, 113)),
                },
            };
            row.Children.Add(chip);
            Grid.SetColumn(chip, 2);

            sp.Children.Add(row);
            first = false;
        }

        card.Child = sp;
        return card;
    }

    // ── selección masiva ──────────────────────────────────────────────────────

    private void SelectAllBtn_Click(object sender, RoutedEventArgs e)
    {
        foreach (var (cb, _) in _checkMap)
            if (cb.IsEnabled) cb.IsChecked = true;
    }

    private void DeselectBtn_Click(object sender, RoutedEventArgs e)
    {
        foreach (var (cb, _) in _checkMap)
            if (cb.IsEnabled) cb.IsChecked = false;
    }

    // ── eliminar ──────────────────────────────────────────────────────────────

    private async void DeleteBtn_Click(object sender, RoutedEventArgs e)
    {
        var toDelete = _checkMap
            .Where(kv => kv.Key.IsEnabled && kv.Key.IsChecked == true)
            .Select(kv => kv.Value).ToList();

        if (toDelete.Count == 0) { await ShowMsg("No hay archivos seleccionados."); return; }

        var dlg = new ContentDialog
        {
            Title             = "Confirmar eliminación",
            Content           = $"Se eliminarán {toDelete.Count} archivo(s) permanentemente. ¿Continuar?",
            PrimaryButtonText = "Eliminar",
            CloseButtonText   = "Cancelar",
            XamlRoot          = XamlRoot,
        };
        if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

        int deleted = 0, failed = 0;
        foreach (var path in toDelete)
        {
            try { File.Delete(path); deleted++; }
            catch { failed++; }
        }

        await ShowMsg($"{deleted} archivo(s) eliminado(s)" + (failed > 0 ? $", {failed} no se pudieron eliminar." : "."));
        ResultsList.Children.Clear();
        _checkMap.Clear();
        ActionBar.Visibility = Visibility.Collapsed;
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

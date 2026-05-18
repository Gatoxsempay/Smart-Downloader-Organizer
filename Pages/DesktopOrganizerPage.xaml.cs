using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using OrganizadorDescargas.Models;
using OrganizadorDescargas.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.UI;

namespace OrganizadorDescargas.Pages;

public sealed partial class DesktopOrganizerPage : Page
{
    private readonly DesktopZoneService _svc = new();
    private List<DesktopIconInfo>? _lastScan;

    public DesktopOrganizerPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await DoScanAsync();
    }

    // ── header buttons ────────────────────────────────────────────────────────

    private async void DiagBtn_Click(object sender, RoutedEventArgs e)
    {
        DiagBtn.IsEnabled = false;
        DiagBtn.Content = "Analizando…";

        string filePath = await _svc.DiagnosticAsync();
        Process.Start(new ProcessStartInfo("notepad.exe", $"\"{filePath}\"") { UseShellExecute = true });

        DiagBtn.IsEnabled = true;
        DiagBtn.Content = "Diagnóstico";
    }

    private async void StressBtn_Click(object sender, RoutedEventArgs e)
    {
        StressBtn.IsEnabled = false;
        StressBtn.Content = "Probando…";

        const int count = 200;
        var result = await _svc.StressTestAsync(count);

        StressBtn.IsEnabled = true;
        StressBtn.Content = "Prueba de estrés";

        var dlg = new ContentDialog
        {
            Title = "Resultado prueba de estrés",
            Content = new StackPanel { Spacing = 8, Children =
            {
                MakeStatRow("Iconos simulados",  $"{result.Requested}"),
                MakeStatRow("Iconos colocados",  $"{result.Arranged}"),
                MakeStatRow("Superposiciones",   $"{result.Overlaps}  {(result.Overlaps == 0 ? "✓" : "⚠")}"),
                MakeStatRow("Tiempo de cálculo", $"{result.ElapsedMs} ms"),
                new TextBlock
                {
                    Text = result.Overlaps == 0
                        ? "El algoritmo maneja 200 iconos sin superposiciones."
                        : "Hay superposiciones: la pantalla no tiene espacio suficiente para todos los iconos.",
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                },
            }},
            CloseButtonText = "Cerrar",
            XamlRoot = XamlRoot,
        };
        await dlg.ShowAsync();
    }

    private static StackPanel MakeStatRow(string label, string value)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        sp.Children.Add(new TextBlock { Text = label + ":", FontSize = 13, Width = 180 });
        sp.Children.Add(new TextBlock { Text = value, FontSize = 13, FontWeight = FontWeights.SemiBold });
        return sp;
    }

    // ── scan ─────────────────────────────────────────────────────────────────

    private async void ScanBtn_Click(object sender, RoutedEventArgs e) => await DoScanAsync();

    private async System.Threading.Tasks.Task DoScanAsync()
    {
        ScanBtn.IsEnabled = false;
        ScanRing.Visibility = Visibility.Visible;

        try
        {
            var (icons, autoArrange) = await _svc.ScanAsync();
            _lastScan = icons;

            AutoArrangeWarning.Visibility = autoArrange ? Visibility.Visible : Visibility.Collapsed;

            TotalLabel.Text  = icons.Count.ToString();
            AppsLabel.Text   = icons.Count(i => i.Zone == DesktopZone.Apps).ToString();
            DevLabel.Text    = icons.Count(i => i.Zone == DesktopZone.Dev).ToString();
            DocsLabel.Text   = icons.Count(i => i.Zone == DesktopZone.Documents).ToString();
            MediaLabel.Text  = icons.Count(i => i.Zone == DesktopZone.Media).ToString();
            OtherLabel.Text  = icons.Count(i => i.Zone == DesktopZone.Other).ToString();
            StatsBar.Visibility = Visibility.Visible;

            BuildIconList(icons);
            ListSection.Visibility = Visibility.Visible;
            ModeBar.Visibility     = Visibility.Visible;

            ArrangeBtn.Content    = "Organizar en zonas";
            ArrangeBtn.Background = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129));
            ArrangeBtn.IsEnabled  = icons.Count > 0;
            PreviewBtn.IsEnabled  = icons.Count > 0;
            RestoreBtn.Visibility = Visibility.Collapsed;
            ActionBar.Visibility  = icons.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            var dlg = new ContentDialog
            {
                Title = "Error al escanear",
                Content = ex.Message,
                CloseButtonText = "OK",
                XamlRoot = XamlRoot,
            };
            await dlg.ShowAsync();
        }
        finally
        {
            ScanBtn.IsEnabled   = true;
            ScanRing.Visibility = Visibility.Collapsed;
        }
    }

    // ── arrange ───────────────────────────────────────────────────────────────

    private async void ArrangeBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_lastScan == null || _lastScan.Count == 0) return;

        ArrangeBtn.IsEnabled = false;
        ArrangeBtn.Content = "Organizando…";

        int moved = await _svc.ArrangeAsync(_lastScan, GetSelectedMode());

        ArrangeBtn.Content = $"✓  {moved} icono{(moved == 1 ? "" : "s")} organizado{(moved == 1 ? "" : "s")}";
        ArrangeBtn.Background = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235));
        RestoreBtn.Visibility = Visibility.Visible;
    }

    private async void PreviewBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_lastScan == null || _lastScan.Count == 0) return;
        await ShowPreviewAsync(_lastScan, GetSelectedMode());
    }

    private async void RestoreBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_lastScan == null) return;

        RestoreBtn.IsEnabled = false;
        RestoreBtn.Content = "Restaurando…";

        await _svc.RestoreAsync(_lastScan);

        RestoreBtn.Content = "Restaurar posiciones";
        RestoreBtn.IsEnabled = true;
        ArrangeBtn.Content = "Organizar en zonas";
        ArrangeBtn.Background = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129));
        ArrangeBtn.IsEnabled = true;
    }

    // ── preview dialog ────────────────────────────────────────────────────────

    private async System.Threading.Tasks.Task ShowPreviewAsync(List<DesktopIconInfo> icons, OrganizationMode mode)
    {
        var layout      = _svc.GetDesktopLayoutInfo();
        var arrangement = _svc.ComputeArrangement(icons, mode);

        const double cW = 840, cH = 472;
        double sx = cW / layout.ScreenW;
        double sy = cH / layout.ScreenH;

        var canvas = new Canvas
        {
            Width  = cW,
            Height = cH,
            Clip   = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, cW, cH) },
        };

        // Wallpaper background
        var wpPath = WallpaperService.GetWallpaperPath();
        if (wpPath != null && File.Exists(wpPath))
        {
            var img = new Image
            {
                Width = cW, Height = cH,
                Stretch = Stretch.UniformToFill,
                Source  = new BitmapImage(new Uri(wpPath)),
            };
            canvas.Children.Add(img);
        }
        else
        {
            canvas.Children.Add(new Rectangle
            {
                Width = cW, Height = cH,
                Fill  = new SolidColorBrush(Color.FromArgb(255, 20, 20, 30)),
            });
        }

        // Semi-dark overlay so zone colors are readable
        canvas.Children.Add(new Rectangle
        {
            Width = cW, Height = cH,
            Fill  = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)),
        });

        // Zone rectangles
        foreach (var zone in layout.Zones)
        {
            var col  = ZoneColor(zone.Zone, 60);
            var col2 = ZoneColor(zone.Zone, 180);

            var rect = new Rectangle
            {
                Width           = zone.Width  * sx,
                Height          = zone.Height * sy,
                Fill            = new SolidColorBrush(col),
                Stroke          = new SolidColorBrush(col2),
                StrokeThickness = 1,
                RadiusX = 4, RadiusY = 4,
            };
            Canvas.SetLeft(rect, zone.X * sx);
            Canvas.SetTop(rect,  zone.Y * sy);
            canvas.Children.Add(rect);

            var lbl = new TextBlock
            {
                Text       = ZoneLabel(zone.Zone),
                FontSize   = 8,
                Foreground = new SolidColorBrush(ZoneColor(zone.Zone, 255)),
                FontWeight = FontWeights.SemiBold,
            };
            Canvas.SetLeft(lbl, zone.X * sx + 4);
            Canvas.SetTop(lbl,  zone.Y * sy + 4);
            canvas.Children.Add(lbl);
        }

        // Icon dots
        foreach (var (_, zone, x, y) in arrangement)
        {
            var dot = new Ellipse
            {
                Width  = 7, Height = 7,
                Fill   = new SolidColorBrush(ZoneColor(zone, 230)),
            };
            Canvas.SetLeft(dot, x * sx - 3);
            Canvas.SetTop(dot,  y * sy - 3);
            canvas.Children.Add(dot);
        }

        var dlg = new ContentDialog
        {
            Title          = $"Vista previa — {ModeName(mode)}",
            Content        = canvas,
            CloseButtonText = "Cerrar",
            XamlRoot       = XamlRoot,
        };
        dlg.Resources["ContentDialogMaxWidth"]  = cW + 80;
        dlg.Resources["ContentDialogMaxHeight"] = cH + 160;

        await dlg.ShowAsync();
    }

    // ── icon list ─────────────────────────────────────────────────────────────

    private void BuildIconList(List<DesktopIconInfo> icons)
    {
        IconsList.Children.Clear();

        var ordered = icons.OrderBy(i => i.Zone).ThenBy(i => i.Name);

        foreach (var icon in ordered)
        {
            var row = new Grid { ColumnSpacing = 10 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            row.Children.Add(new TextBlock
            {
                Text = icon.Name,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });

            var chip = new Border
            {
                CornerRadius = new CornerRadius(10),
                Background   = new SolidColorBrush(ZoneColor(icon.Zone, 60)),
                Padding      = new Thickness(8, 2, 8, 2),
                VerticalAlignment = VerticalAlignment.Center,
            };
            chip.Child = new TextBlock
            {
                Text       = ZoneLabel(icon.Zone),
                FontSize   = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(ZoneColor(icon.Zone, 255)),
            };
            Grid.SetColumn(chip, 1);
            row.Children.Add(chip);

            IconsList.Children.Add(row);
        }

        if (icons.Count == 0)
        {
            IconsList.Children.Add(new TextBlock
            {
                Text = "No se encontraron iconos en el escritorio.",
                FontSize = 12,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });
        }
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private OrganizationMode GetSelectedMode() =>
        (ModeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() switch
        {
            "Alphabetical" => OrganizationMode.Alphabetical,
            "ByExtension"  => OrganizationMode.ByExtension,
            _              => OrganizationMode.ByZone,
        };

    private static string ModeName(OrganizationMode m) => m switch
    {
        OrganizationMode.Alphabetical => "Alfabético",
        OrganizationMode.ByExtension  => "Por extensión",
        _                             => "Por zona",
    };

    private static string ZoneLabel(DesktopZone z) => z switch
    {
        DesktopZone.Apps      => "App",
        DesktopZone.Dev       => "Dev",
        DesktopZone.Documents => "Doc",
        DesktopZone.Media     => "Media",
        _                     => "Carpeta",
    };

    private static Color ZoneColor(DesktopZone z, byte alpha) => z switch
    {
        DesktopZone.Apps      => Color.FromArgb(alpha, 168,  85, 247),
        DesktopZone.Dev       => Color.FromArgb(alpha,   6, 182, 212),
        DesktopZone.Documents => Color.FromArgb(alpha,  96, 165, 250),
        DesktopZone.Media     => Color.FromArgb(alpha, 252, 211,  77),
        _                     => Color.FromArgb(alpha, 156, 163, 175),
    };
}

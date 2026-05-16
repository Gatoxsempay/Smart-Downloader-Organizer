using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using OrganizadorDescargas.Services;
using System;
using Windows.UI;

namespace OrganizadorDescargas.Pages;

public sealed partial class DashboardPage : Page
{
    private bool _isRunning;
    private int _today;

    public DashboardPage()
    {
        InitializeComponent();
        var cfg = MainWindow.Config.Get();
        FolderLabel.Text = cfg.MonitoredFolder;
        TotalLabel.Text = cfg.Stats.TotalMoved.ToString();
        CatsLabel.Text = cfg.Stats.ByCategory.Count.ToString();
        UpdateStatus(MainWindow.Organizer.IsRunning);
    }

    public void UpdateStatus(bool running)
    {
        _isRunning = running;
        var color = running
            ? Color.FromArgb(255, 16, 185, 129)
            : Color.FromArgb(255, 150, 150, 150);
        StatusDot.Fill = new SolidColorBrush(color);
        StatusLabel.Foreground = new SolidColorBrush(color);
        StatusLabel.Text = running ? "Activo" : "Detenido";
        ToggleBtn.Content = running ? "Detener" : "Iniciar";
        ToggleBtn.Background = running
            ? new SolidColorBrush(Color.FromArgb(255, 220, 38, 38))
            : new SolidColorBrush(Color.FromArgb(255, 37, 99, 235));
    }

    public void OnFileMoved(FileMoved data)
    {
        _today++;
        TodayLabel.Text = _today.ToString();
        var stats = MainWindow.Config.Get().Stats;
        TotalLabel.Text = stats.TotalMoved.ToString();
        CatsLabel.Text = stats.ByCategory.Count.ToString();
    }

    public void AppendActivity(LogEntry entry)
    {
        if (!entry.Message.Contains("Movido:")) return;
        EmptyLabel.Visibility = Visibility.Collapsed;

        var parts = entry.Message.Split('→');
        var filename = parts[0].Replace("Movido:", "").Trim();
        var category = parts.Length > 1 ? parts[1].Trim().Split('/')[0].Trim() : "";

        var row = new Border
        {
            CornerRadius = new CornerRadius(8),
            Height = 44,
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            Margin = new Thickness(0, 0, 0, 2),
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var time = new TextBlock
        {
            Text = entry.Time.ToString("HH:mm"),
            FontSize = 11,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 4, 0),
        };
        Grid.SetColumn(time, 0);

        var badge = new Border
        {
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 2, 6, 2),
            Background = CategoryBadgeBg(category),
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        badge.Child = new TextBlock
        {
            Text = category,
            FontSize = 10,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = CategoryBadgeFg(category),
        };
        Grid.SetColumn(badge, 1);

        var file = new TextBlock
        {
            Text = filename,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 0, 12, 0),
        };
        Grid.SetColumn(file, 2);

        grid.Children.Add(time);
        if (!string.IsNullOrEmpty(category)) grid.Children.Add(badge);
        grid.Children.Add(file);
        row.Child = grid;

        FeedPanel.Children.Insert(0, row);
        if (FeedPanel.Children.Count > 62) // keep EmptyLabel slot
            FeedPanel.Children.RemoveAt(FeedPanel.Children.Count - 1);
    }

    private static SolidColorBrush CategoryBadgeBg(string cat) => cat switch
    {
        "Documentos"   => new SolidColorBrush(Color.FromArgb(40, 29, 78, 216)),
        "Imágenes"     => new SolidColorBrush(Color.FromArgb(40, 109, 40, 217)),
        "Videos"       => new SolidColorBrush(Color.FromArgb(40, 190, 24, 93)),
        "Audio"        => new SolidColorBrush(Color.FromArgb(40, 180, 83, 9)),
        "Instaladores" => new SolidColorBrush(Color.FromArgb(40, 185, 28, 28)),
        "Comprimidos"  => new SolidColorBrush(Color.FromArgb(40, 4, 120, 87)),
        _ => new SolidColorBrush(Color.FromArgb(40, 120, 120, 120)),
    };

    private static SolidColorBrush CategoryBadgeFg(string cat) => cat switch
    {
        "Documentos"   => new SolidColorBrush(Color.FromArgb(255, 147, 197, 253)),
        "Imágenes"     => new SolidColorBrush(Color.FromArgb(255, 196, 181, 253)),
        "Videos"       => new SolidColorBrush(Color.FromArgb(255, 249, 168, 212)),
        "Audio"        => new SolidColorBrush(Color.FromArgb(255, 252, 211, 77)),
        "Instaladores" => new SolidColorBrush(Color.FromArgb(255, 252, 165, 165)),
        "Comprimidos"  => new SolidColorBrush(Color.FromArgb(255, 110, 231, 183)),
        _ => new SolidColorBrush(Colors.White),
    };

    private void ToggleBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning) MainWindow.Organizer.Stop();
        else MainWindow.Organizer.Start();
    }

    private void OrganizeNowBtn_Click(object sender, RoutedEventArgs e)
    {
        MainWindow.Organizer.OrganizeNow();
    }
}

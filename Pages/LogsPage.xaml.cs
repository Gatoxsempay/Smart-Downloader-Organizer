using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using OrganizadorDescargas.Services;
using Windows.UI;

namespace OrganizadorDescargas.Pages;

public sealed partial class LogsPage : Page
{
    public LogsPage()
    {
        InitializeComponent();
    }

    public void AppendLog(LogEntry entry)
    {
        EmptyLabel.Visibility = Visibility.Collapsed;

        var row = new TextBlock
        {
            Text = $"[{entry.Time:HH:mm:ss}]  {entry.Message}",
            FontSize = 12,
            FontFamily = new FontFamily("Consolas"),
            TextWrapping = TextWrapping.Wrap,
            Foreground = entry.IsError
                ? new SolidColorBrush(Color.FromArgb(255, 252, 165, 165))
                : (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"],
        };

        LogPanel.Children.Insert(0, row);

        if (LogPanel.Children.Count > 201)
            LogPanel.Children.RemoveAt(LogPanel.Children.Count - 1);
    }

    private void ClearBtn_Click(object sender, RoutedEventArgs e)
    {
        LogPanel.Children.Clear();
        LogPanel.Children.Add(EmptyLabel);
        EmptyLabel.Visibility = Visibility.Visible;
    }
}

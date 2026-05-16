using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using OrganizadorDescargas.Models;
using System.Linq;
using Windows.UI;

namespace OrganizadorDescargas.Pages;

internal static class OrganizerPageHelper
{
    internal static void BuildPreview(StackPanel previewPanel, ScanResult scan)
    {
        previewPanel.Children.Clear();

        foreach (var category in scan.Categories)
        {
            var files = scan.Files.Where(f => f.Category == category).ToList();
            var card = new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 10, 14, 10),
            };
            var stack = new StackPanel { Spacing = 4 };

            var headerRow = new Grid();
            headerRow.Children.Add(new TextBlock
            {
                Text = category,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 13,
            });
            var badge = new Border
            {
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(50, 37, 99, 235)),
                Padding = new Thickness(8, 2, 8, 2),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
            };
            badge.Child = new TextBlock
            {
                Text = files.Count.ToString(),
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 147, 197, 253)),
            };
            headerRow.Children.Add(badge);
            stack.Children.Add(headerRow);

            foreach (var item in files.Take(6))
                stack.Children.Add(FileRow(item.FileName));

            if (files.Count > 6)
                stack.Children.Add(new TextBlock
                {
                    Text = $"  … y {files.Count - 6} más",
                    FontSize = 12,
                    Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                    FontStyle = Windows.UI.Text.FontStyle.Italic,
                });

            card.Child = stack;
            previewPanel.Children.Add(card);
        }

        var noRule = scan.Files.Where(f => f.Category == null).ToList();
        if (noRule.Count == 0) return;

        var noCard = new Border
        {
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.FromArgb(15, 245, 158, 11)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(30, 245, 158, 11)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(14, 10, 14, 10),
        };
        var noStack = new StackPanel { Spacing = 4 };
        noStack.Children.Add(new TextBlock
        {
            Text = $"Sin categoría  ({noRule.Count})",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 245, 158, 11)),
        });
        foreach (var item in noRule.Take(6))
            noStack.Children.Add(FileRow(item.FileName));
        if (noRule.Count > 6)
            noStack.Children.Add(new TextBlock
            {
                Text = $"  … y {noRule.Count - 6} más",
                FontSize = 12,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                FontStyle = Windows.UI.Text.FontStyle.Italic,
            });
        noCard.Child = noStack;
        previewPanel.Children.Add(noCard);
    }

    private static TextBlock FileRow(string name) => new()
    {
        Text = $"  {name}",
        FontSize = 12,
        Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
        TextTrimming = TextTrimming.CharacterEllipsis,
        Margin = new Thickness(0, 1, 0, 1),
    };
}

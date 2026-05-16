using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;

namespace OrganizadorDescargas.Pages;

public sealed partial class ExclusionsPage : Page
{
    public ExclusionsPage()
    {
        InitializeComponent();
        Refresh();
    }

    private void Refresh()
    {
        var cfg = MainWindow.Config.Get();
        RebuildList(ExtList, cfg.ExcludedExtensions, RemoveExt);
        RebuildList(NameList, cfg.ExcludedNames, RemoveName);
    }

    private void RebuildList(ItemsControl list, List<string> items, System.Action<string> onRemove)
    {
        var panel = new StackPanel { Spacing = 4 };
        foreach (var item in items.OrderBy(x => x))
        {
            var row = new Grid { Height = 36 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var lbl = new TextBlock
            {
                Text = item,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Courier New"),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(lbl, 0);

            var rm = new Button { Content = "✕" };
            var captured = item;
            rm.Click += (s, e) => { onRemove(captured); Refresh(); };
            Grid.SetColumn(rm, 1);

            row.Children.Add(lbl);
            row.Children.Add(rm);
            panel.Children.Add(row);
        }
        list.ItemsSource = null;
        list.Items.Clear();
        list.Items.Add(panel);
    }

    private void AddExt_Click(object sender, RoutedEventArgs e)
    {
        var newItems = ExtBox.Text.Split(',')
            .Select(x => x.Trim().ToLower())
            .Where(x => x.StartsWith('.') && !string.IsNullOrEmpty(x))
            .ToList();
        if (newItems.Count == 0) return;
        MainWindow.Config.Update(cfg =>
        {
            foreach (var item in newItems)
                if (!cfg.ExcludedExtensions.Contains(item))
                    cfg.ExcludedExtensions.Add(item);
        });
        ExtBox.Text = "";
        Refresh();
    }

    private void AddName_Click(object sender, RoutedEventArgs e)
    {
        var newItems = NameBox.Text.Split(',')
            .Select(x => x.Trim().ToLower())
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();
        if (newItems.Count == 0) return;
        MainWindow.Config.Update(cfg =>
        {
            foreach (var item in newItems)
                if (!cfg.ExcludedNames.Contains(item))
                    cfg.ExcludedNames.Add(item);
        });
        NameBox.Text = "";
        Refresh();
    }

    private void RemoveExt(string item)
        => MainWindow.Config.Update(cfg => cfg.ExcludedExtensions.Remove(item));

    private void RemoveName(string item)
        => MainWindow.Config.Update(cfg => cfg.ExcludedNames.Remove(item));
}

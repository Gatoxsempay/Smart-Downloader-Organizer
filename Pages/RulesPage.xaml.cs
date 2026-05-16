using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using OrganizadorDescargas.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;

namespace OrganizadorDescargas.Pages;

public sealed partial class RulesPage : Page
{
    public RulesPage()
    {
        InitializeComponent();
        Refresh();
    }

    private void Refresh()
    {
        FolderList.Children.Clear();
        var q = SearchBox?.Text?.ToLower() ?? "";
        var rules = MainWindow.Config.Get().ExtensionRules;

        var groups = new Dictionary<string, List<string>>();
        foreach (var (ext, folder) in rules)
        {
            if (!string.IsNullOrEmpty(q) && !folder.ToLower().Contains(q) && !ext.ToLower().Contains(q))
                continue;
            if (!groups.ContainsKey(folder)) groups[folder] = [];
            groups[folder].Add(ext);
        }

        var sorted = groups.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value.OrderBy(x => x).ToList());
        var totalExt = sorted.Values.Sum(v => v.Count);
        CountLabel.Text = $"{sorted.Count} carpeta(s)  ·  {totalExt} extensión(es)";

        if (sorted.Count == 0)
        {
            FolderList.Children.Add(EmptyLabel);
            EmptyLabel.Visibility = Visibility.Visible;
            return;
        }
        EmptyLabel.Visibility = Visibility.Collapsed;
        foreach (var (folder, exts) in sorted)
            FolderList.Children.Add(BuildFolderCard(folder, exts));
    }

    private UIElement BuildFolderCard(string folder, List<string> exts)
    {
        var card = new Border
        {
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(18, 14, 18, 14),
        };

        var stack = new StackPanel { Spacing = 10 };

        // Top row
        var topRow = new Grid();
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var folderName = new TextBlock
        {
            Text = folder,
            FontSize = 15,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(folderName, 0);

        var badge = new TextBlock
        {
            Text = $"{exts.Count} ext.",
            FontSize = 11,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 16, 0),
        };
        Grid.SetColumn(badge, 1);

        var manageBtn = new Button
        {
            Content = "Gestionar extensiones",
            Margin = new Thickness(0, 0, 6, 0),
        };
        manageBtn.Click += (s, e) => ShowManageDialog(folder);
        Grid.SetColumn(manageBtn, 2);

        var deleteBtn = new Button { Content = "Eliminar carpeta" };
        deleteBtn.Click += (s, e) => DeleteFolder(folder);
        Grid.SetColumn(deleteBtn, 3);

        topRow.Children.Add(folderName);
        topRow.Children.Add(badge);
        topRow.Children.Add(manageBtn);
        topRow.Children.Add(deleteBtn);
        stack.Children.Add(topRow);

        // Divider
        stack.Children.Add(new Border { Height = 1, Background = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"] });

        // Extension chips
        var chips = new CommunityToolkit.WinUI.Controls.WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var ext in exts.Take(12))
        {
            chips.Children.Add(new Border
            {
                Background = ChipBg(folder),
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(3, 2, 3, 2),
                Padding = new Thickness(6, 2, 6, 2),
                Child = new TextBlock
                {
                    Text = ext,
                    FontSize = 12,
                    FontFamily = new FontFamily("Courier New"),
                    Foreground = ChipFg(folder),
                },
            });
        }
        if (exts.Count > 12)
            chips.Children.Add(new TextBlock
            {
                Text = $"  +{exts.Count - 12} más",
                FontSize = 12,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 2, 0, 2),
            });

        stack.Children.Add(chips);
        card.Child = stack;
        return card;
    }

    private async void ShowManageDialog(string folder)
    {
        var dlg = new ContentDialog
        {
            Title = $"Extensiones — {folder}",
            CloseButtonText = "Cerrar",
            XamlRoot = XamlRoot,
        };

        var panel = new StackPanel { Spacing = 8, Width = 400 };

        void RebuildList()
        {
            panel.Children.Clear();
            var current = GetExtsForFolder(folder);
            if (current.Count == 0)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "(sin extensiones — esta carpeta será eliminada)",
                    Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                });
                return;
            }
            foreach (var ext in current.OrderBy(x => x))
            {
                var row = new Grid { Height = 40 };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var lbl = new TextBlock
                {
                    Text = ext, VerticalAlignment = VerticalAlignment.Center,
                    FontFamily = new FontFamily("Courier New"),
                };
                Grid.SetColumn(lbl, 0);
                var rm = new Button { Content = "✕" };
                rm.Click += (s, e) =>
                {
                    MainWindow.Config.Update(cfg => cfg.ExtensionRules.Remove(ext));
                    RebuildList();
                    Refresh();
                };
                Grid.SetColumn(rm, 1);
                row.Children.Add(lbl);
                row.Children.Add(rm);
                panel.Children.Add(row);
            }
        }
        RebuildList();

        // Add extensions row
        var addBox = new TextBox { PlaceholderText = ".mcaddon, .mcpack" };
        var addBtn = new Button { Content = "Agregar" };
        addBtn.Click += (s, e) =>
        {
            var newExts = addBox.Text.Split(',')
                .Select(x => x.Trim().ToLower())
                .Where(x => x.StartsWith('.'))
                .ToList();
            if (newExts.Count == 0) return;
            MainWindow.Config.Update(cfg =>
            {
                foreach (var ext in newExts)
                    cfg.ExtensionRules[ext] = folder;
            });
            addBox.Text = "";
            RebuildList();
            Refresh();
        };

        var addRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 12, 0, 0) };
        addRow.Children.Add(addBox);
        addRow.Children.Add(addBtn);

        var content = new StackPanel { Spacing = 4 };
        content.Children.Add(new ScrollViewer { Content = panel, MaxHeight = 300 });
        content.Children.Add(new Border { Height = 1, Background = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"], Margin = new Thickness(0, 8, 0, 8) });
        content.Children.Add(new TextBlock { Text = "Agregar extensiones (separadas por comas):" });
        content.Children.Add(addRow);

        dlg.Content = content;
        await dlg.ShowAsync();
    }

    private async void DeleteFolder(string folder)
    {
        var exts = GetExtsForFolder(folder);
        var dlg = new ContentDialog
        {
            Title = "Eliminar carpeta",
            Content = $"¿Eliminar '{folder}' y sus {exts.Count} extensión(es)?\n{string.Join(", ", exts.OrderBy(x => x))}",
            PrimaryButtonText = "Eliminar",
            CloseButtonText = "Cancelar",
            XamlRoot = XamlRoot,
        };
        if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;
        MainWindow.Config.Update(cfg =>
        {
            foreach (var ext in exts) cfg.ExtensionRules.Remove(ext);
        });
        Refresh();
    }

    private async void NewFolderBtn_Click(object sender, RoutedEventArgs e)
    {
        var nameBox = new TextBox { PlaceholderText = "Mis Mods", Margin = new Thickness(0, 0, 0, 8) };
        var extBox = new TextBox { PlaceholderText = ".mcaddon, .mcpack, .mcworld" };
        var content = new StackPanel { Spacing = 4 };
        content.Children.Add(new TextBlock { Text = "Nombre de la carpeta:" });
        content.Children.Add(nameBox);
        content.Children.Add(new TextBlock { Text = "Extensiones (separadas por comas):", Margin = new Thickness(0, 8, 0, 0) });
        content.Children.Add(extBox);

        var dlg = new ContentDialog
        {
            Title = "Nueva carpeta",
            Content = content,
            PrimaryButtonText = "Crear",
            CloseButtonText = "Cancelar",
            XamlRoot = XamlRoot,
        };
        if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

        var folder = nameBox.Text.Trim();
        if (string.IsNullOrEmpty(folder)) return;
        var newExts = extBox.Text.Split(',').Select(x => x.Trim().ToLower()).Where(x => x.StartsWith('.')).ToList();
        if (newExts.Count == 0) return;

        MainWindow.Config.Update(cfg =>
        {
            foreach (var ext in newExts) cfg.ExtensionRules[ext] = folder;
        });
        Refresh();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => Refresh();

    private List<string> GetExtsForFolder(string folder)
    {
        var rules = MainWindow.Config.Get().ExtensionRules;
        return [.. rules.Where(kv => kv.Value == folder).Select(kv => kv.Key)];
    }

    private static SolidColorBrush ChipBg(string folder) => folder switch
    {
        "Documentos"   => new SolidColorBrush(Color.FromArgb(40, 29, 78, 216)),
        "Imágenes"     => new SolidColorBrush(Color.FromArgb(40, 109, 40, 217)),
        "Videos"       => new SolidColorBrush(Color.FromArgb(40, 190, 24, 93)),
        "Audio"        => new SolidColorBrush(Color.FromArgb(40, 180, 83, 9)),
        "Instaladores" => new SolidColorBrush(Color.FromArgb(40, 185, 28, 28)),
        "Comprimidos"  => new SolidColorBrush(Color.FromArgb(40, 4, 120, 87)),
        _ => new SolidColorBrush(Color.FromArgb(40, 120, 120, 120)),
    };

    private static SolidColorBrush ChipFg(string folder) => folder switch
    {
        "Documentos"   => new SolidColorBrush(Color.FromArgb(255, 147, 197, 253)),
        "Imágenes"     => new SolidColorBrush(Color.FromArgb(255, 196, 181, 253)),
        "Videos"       => new SolidColorBrush(Color.FromArgb(255, 249, 168, 212)),
        "Audio"        => new SolidColorBrush(Color.FromArgb(255, 252, 211, 77)),
        "Instaladores" => new SolidColorBrush(Color.FromArgb(255, 252, 165, 165)),
        "Comprimidos"  => new SolidColorBrush(Color.FromArgb(255, 110, 231, 183)),
        _ => new SolidColorBrush(Color.FromArgb(255, 200, 200, 200)),
    };
}

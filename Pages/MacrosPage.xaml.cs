using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using OrganizadorDescargas.Models;
using OrganizadorDescargas.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.UI;

namespace OrganizadorDescargas.Pages;

public sealed partial class MacrosPage : Page
{
    private readonly MacroService          _svc    = new();
    private          List<MacroDefinition> _macros = new();

    // Action types shown to the user (in order)
    private static readonly (MacroActionType Type, string Label, bool NeedsParam, bool NeedsBrowse)[] ActionTypes =
    [
        (MacroActionType.OpenFolder,      "Abrir una carpeta",              true,  true),
        (MacroActionType.OpenApp,         "Abrir un programa",              true,  true),
        (MacroActionType.OpenUrl,         "Abrir un sitio web",             true,  false),
        (MacroActionType.CleanTemp,       "Limpiar archivos temporales",    false, false),
        (MacroActionType.EmptyRecycleBin, "Vaciar la papelera de reciclaje",false, false),
        (MacroActionType.Shutdown,        "Apagar el equipo",               false, false),
        (MacroActionType.Restart,         "Reiniciar el equipo",            false, false),
        (MacroActionType.Sleep,           "Suspender el equipo",            false, false),
        (MacroActionType.LockScreen,      "Bloquear la pantalla",           false, false),
    ];

    public MacrosPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Reload();
    }

    private void Reload()
    {
        _macros = _svc.Load();
        BuildCards();
    }

    // ── tarjetas ──────────────────────────────────────────────────────────────

    private void BuildCards()
    {
        MacrosList.Children.Clear();

        if (_macros.Count == 0)
        {
            MacrosList.Children.Add(new TextBlock
            {
                Text = "No hay acciones rápidas. Crea una con el botón de arriba.",
                FontSize = 13,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });
            return;
        }

        foreach (var macro in _macros)
            MacrosList.Children.Add(MakeCard(macro));
    }

    private Border MakeCard(MacroDefinition macro)
    {
        var card = new Border
        {
            Background    = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            CornerRadius  = new CornerRadius(12),
            Padding       = new Thickness(20, 16, 20, 16),
        };

        var root = new Grid { ColumnSpacing = 12 };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Icon
        var iconBorder = new Border
        {
            Width = 44, Height = 44,
            CornerRadius = new CornerRadius(10),
            Background   = new SolidColorBrush(Color.FromArgb(30, 37, 99, 235)),
            Child = new FontIcon
            {
                Glyph = macro.Glyph,
                FontSize = 20,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 96, 165, 250)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
            },
        };
        root.Children.Add(iconBorder);
        Grid.SetColumn(iconBorder, 0);

        // Name + step summary
        var info = new StackPanel { Spacing = 3, VerticalAlignment = VerticalAlignment.Center };
        info.Children.Add(new TextBlock { Text = macro.Name, FontSize = 14, FontWeight = FontWeights.SemiBold });

        string preview = macro.Actions.Count == 0
            ? "(sin pasos)"
            : macro.Actions.Count == 1
                ? macro.Actions[0].GetLabel()
                : $"{macro.Actions.Count} pasos: {string.Join(", ", macro.Actions.Select(a => a.GetLabel()))}";

        info.Children.Add(new TextBlock
        {
            Text = preview,
            FontSize = 11,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
        });
        root.Children.Add(info);
        Grid.SetColumn(info, 1);

        // Action buttons
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var runBtn = new Button
        {
            Content = "Ejecutar",
            Background = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129)),
            Foreground = new SolidColorBrush(Colors.White),
            FontWeight = FontWeights.SemiBold,
            Padding = new Thickness(12, 6, 12, 6),
            Tag = macro,
        };
        runBtn.Click += RunBtn_Click;
        actions.Children.Add(runBtn);

        var editBtn = new Button
        {
            Content = "Editar",
            Background = new SolidColorBrush(Color.FromArgb(255, 55, 65, 81)),
            Foreground = new SolidColorBrush(Colors.White),
            Padding = new Thickness(12, 6, 12, 6),
            Tag = macro,
        };
        editBtn.Click += EditBtn_Click;
        actions.Children.Add(editBtn);

        var delBtn = new Button
        {
            Content = "Eliminar",
            Background = new SolidColorBrush(Color.FromArgb(255, 185, 28, 28)),
            Foreground = new SolidColorBrush(Colors.White),
            Padding = new Thickness(12, 6, 12, 6),
            Tag = macro,
        };
        delBtn.Click += DeleteBtn_Click;
        actions.Children.Add(delBtn);

        root.Children.Add(actions);
        Grid.SetColumn(actions, 2);

        card.Child = root;
        return card;
    }

    // ── ejecutar ──────────────────────────────────────────────────────────────

    private async void RunBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not MacroDefinition macro) return;

        btn.IsEnabled = false;
        btn.Content   = "Ejecutando…";

        var (exitCode, output) = await _svc.RunAsync(macro);

        btn.IsEnabled = true;
        btn.Content   = "Ejecutar";

        string resultText = string.IsNullOrWhiteSpace(output)
            ? exitCode == 0 ? "✓ Completado correctamente." : $"Salió con código {exitCode}."
            : output;

        var dlg = new ContentDialog
        {
            Title           = $"Resultado: {macro.Name}",
            Content         = new TextBox
            {
                Text          = resultText,
                IsReadOnly    = true,
                TextWrapping  = TextWrapping.Wrap,
                AcceptsReturn = true,
                MaxHeight     = 280,
                FontFamily    = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                FontSize      = 11,
            },
            CloseButtonText = "Cerrar",
            XamlRoot        = XamlRoot,
        };
        dlg.Resources["ContentDialogMaxWidth"] = 680.0;
        await dlg.ShowAsync();
    }

    // ── añadir / editar ───────────────────────────────────────────────────────

    private async void AddMacroBtn_Click(object sender, RoutedEventArgs e)
    {
        var newMacro = new MacroDefinition();
        if (await ShowEditDialog(newMacro, "Nueva acción rápida"))
        {
            _macros.Add(newMacro);
            _svc.Save(_macros);
            BuildCards();
        }
    }

    private async void EditBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not MacroDefinition macro) return;

        var copy = new MacroDefinition
        {
            Id      = macro.Id,
            Name    = macro.Name,
            Glyph   = macro.Glyph,
            Actions = macro.Actions.Select(a => new MacroAction { Type = a.Type, Param = a.Param }).ToList(),
        };

        if (await ShowEditDialog(copy, "Editar acción rápida"))
        {
            int idx = _macros.FindIndex(m => m.Id == copy.Id);
            if (idx >= 0) _macros[idx] = copy;
            _svc.Save(_macros);
            BuildCards();
        }
    }

    private async Task<bool> ShowEditDialog(MacroDefinition macro, string title)
    {
        // ── name ────────────────────────────────────────────────────────────
        var nameBox = new TextBox
        {
            PlaceholderText = "Nombre de la acción rápida…",
            Text = macro.Name is "Nueva acción rápida" ? "" : macro.Name,
            Margin = new Thickness(0, 0, 0, 4),
        };

        // ── steps list (mutable working copy) ───────────────────────────────
        var currentActions = macro.Actions.Select(a => new MacroAction { Type = a.Type, Param = a.Param }).ToList();
        var stepsPanel = new StackPanel { Spacing = 4 };

        void RebuildSteps()
        {
            stepsPanel.Children.Clear();
            if (currentActions.Count == 0)
            {
                stepsPanel.Children.Add(new TextBlock
                {
                    Text = "Sin pasos todavía. Añade uno abajo.",
                    FontSize = 12,
                    Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                    Margin = new Thickness(0, 4, 0, 4),
                });
                return;
            }

            for (int i = 0; i < currentActions.Count; i++)
            {
                var action = currentActions[i];
                int capturedIdx = i;

                var row = new Grid { ColumnSpacing = 8, Margin = new Thickness(0) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var icon = new FontIcon { Glyph = action.GetGlyph(), FontSize = 14, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(icon, 0);

                var lbl = new TextBlock
                {
                    Text = action.GetLabel(),
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                };
                Grid.SetColumn(lbl, 1);

                var removeBtn = new Button
                {
                    Content = "×",
                    Background = new SolidColorBrush(Colors.Transparent),
                    Width = 28, Height = 28,
                    FontSize = 14,
                    Padding = new Thickness(0),
                };
                removeBtn.Click += (_, _) => { currentActions.RemoveAt(capturedIdx); RebuildSteps(); };
                Grid.SetColumn(removeBtn, 2);

                row.Children.Add(icon);
                row.Children.Add(lbl);
                row.Children.Add(removeBtn);

                stepsPanel.Children.Add(new Border
                {
                    Background   = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                    CornerRadius = new CornerRadius(6),
                    Padding      = new Thickness(10, 7, 10, 7),
                    Child        = row,
                });
            }
        }

        RebuildSteps();

        // ── add-step section ─────────────────────────────────────────────────
        var typeCombo = new ComboBox { Width = 300, Margin = new Thickness(0, 0, 0, 0) };
        foreach (var (_, label, _, _) in ActionTypes)
            typeCombo.Items.Add(label);
        typeCombo.SelectedIndex = 0;

        var paramBox = new TextBox
        {
            PlaceholderText = "Ruta de la carpeta…",
            Width = 280,
        };
        var browseBtn = new Button { Content = "Examinar…", Margin = new Thickness(8, 0, 0, 0) };

        var paramRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 6, 0, 0),
            Children = { paramBox, browseBtn },
        };

        var noParamLabel = new TextBlock
        {
            Text = "Esta acción no necesita configuración adicional.",
            FontSize = 12,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            Margin = new Thickness(0, 6, 0, 0),
            Visibility = Visibility.Collapsed,
        };

        void UpdateParamUI()
        {
            int idx = typeCombo.SelectedIndex >= 0 ? typeCombo.SelectedIndex : 0;
            var (type, _, needsParam, needsBrowse) = ActionTypes[idx];

            paramRow.Visibility    = needsParam  ? Visibility.Visible   : Visibility.Collapsed;
            noParamLabel.Visibility = needsParam ? Visibility.Collapsed : Visibility.Visible;
            browseBtn.Visibility   = needsBrowse ? Visibility.Visible   : Visibility.Collapsed;

            paramBox.PlaceholderText = type switch
            {
                MacroActionType.OpenFolder => "Ruta de la carpeta…",
                MacroActionType.OpenApp    => "Ruta del programa (.exe)…",
                MacroActionType.OpenUrl    => "Dirección web (https://…)",
                _                          => "",
            };
            paramBox.Text = "";
        }

        typeCombo.SelectionChanged += (_, _) => UpdateParamUI();
        UpdateParamUI();

        browseBtn.Click += async (_, _) =>
        {
            int idx = typeCombo.SelectedIndex >= 0 ? typeCombo.SelectedIndex : 0;
            var (type, _, _, _) = ActionTypes[idx];
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance!);

            if (type == MacroActionType.OpenFolder)
            {
                var picker = new Windows.Storage.Pickers.FolderPicker();
                picker.FileTypeFilter.Add("*");
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                var folder = await picker.PickSingleFolderAsync();
                if (folder != null) paramBox.Text = folder.Path;
            }
            else
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                picker.FileTypeFilter.Add(".exe");
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                var file = await picker.PickSingleFileAsync();
                if (file != null) paramBox.Text = file.Path;
            }
        };

        var addStepBtn = new Button
        {
            Content    = "+ Añadir este paso",
            Background = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235)),
            Foreground = new SolidColorBrush(Colors.White),
            Padding    = new Thickness(14, 7, 14, 7),
            Margin     = new Thickness(0, 8, 0, 0),
        };
        addStepBtn.Click += (_, _) =>
        {
            int idx = typeCombo.SelectedIndex >= 0 ? typeCombo.SelectedIndex : 0;
            var (type, _, needsParam, _) = ActionTypes[idx];
            string param = paramBox.Text.Trim();
            if (needsParam && string.IsNullOrEmpty(param)) return;

            currentActions.Add(new MacroAction { Type = type, Param = param });
            paramBox.Text = "";
            RebuildSteps();
        };

        // ── layout ───────────────────────────────────────────────────────────
        var separator = new Border
        {
            Height     = 1,
            Background = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"],
            Margin     = new Thickness(0, 14, 0, 10),
        };

        var panel = new StackPanel { Spacing = 0, Width = 420 };
        panel.Children.Add(new TextBlock { Text = "Nombre:", FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
        panel.Children.Add(nameBox);
        panel.Children.Add(new TextBlock { Text = "Pasos a ejecutar:", FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 12, 0, 6) });
        panel.Children.Add(stepsPanel);
        panel.Children.Add(separator);
        panel.Children.Add(new TextBlock { Text = "Añadir un paso:", FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
        panel.Children.Add(typeCombo);
        panel.Children.Add(paramRow);
        panel.Children.Add(noParamLabel);
        panel.Children.Add(addStepBtn);

        var dlg = new ContentDialog
        {
            Title             = title,
            Content           = new ScrollViewer { Content = panel, MaxHeight = 520, VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
            PrimaryButtonText = "Guardar",
            CloseButtonText   = "Cancelar",
            XamlRoot          = XamlRoot,
        };
        dlg.Resources["ContentDialogMaxWidth"] = 560.0;

        var result = await dlg.ShowAsync();
        if (result != ContentDialogResult.Primary) return false;

        string name = nameBox.Text.Trim();
        if (string.IsNullOrEmpty(name) || currentActions.Count == 0) return false;

        macro.Name    = name;
        macro.Actions = currentActions;
        return true;
    }

    // ── eliminar ──────────────────────────────────────────────────────────────

    private async void DeleteBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not MacroDefinition macro) return;

        var dlg = new ContentDialog
        {
            Title             = "Eliminar acción rápida",
            Content           = $"¿Eliminar \"{macro.Name}\"? Esta acción no se puede deshacer.",
            PrimaryButtonText = "Eliminar",
            CloseButtonText   = "Cancelar",
            XamlRoot          = XamlRoot,
        };
        if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

        _macros.Remove(macro);
        _svc.Save(_macros);
        BuildCards();
    }
}

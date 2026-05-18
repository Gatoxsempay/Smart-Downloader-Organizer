using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using OrganizadorDescargas.Models;
using OrganizadorDescargas.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.UI;

namespace OrganizadorDescargas;

public sealed partial class ToolbarWindow : Window
{
    private readonly ToolbarService  _svc    = new();
    private readonly MacroService    _macros = new();
    private ToolbarConfig            _cfg;
    private OverlappedPresenter?     _presenter;
    private bool                     _isCollapsed;
    private const int ExpandedHeight = 56;
    private const int CollapsedSize  = 52;

    public ToolbarWindow()
    {
        InitializeComponent();

        _cfg = _svc.Load();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(RootGrid);

        AppWindow.Resize(new SizeInt32(600, ExpandedHeight));
        AppWindow.Move(new PointInt32(_cfg.X, _cfg.Y));
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;

        _presenter = AppWindow.Presenter as OverlappedPresenter;
        if (_presenter != null)
        {
            _presenter.IsResizable        = false;
            _presenter.IsMaximizable      = false;
            _presenter.IsMinimizable      = false;
            _presenter.SetBorderAndTitleBar(false, false);
            _presenter.IsAlwaysOnTop      = _cfg.AlwaysOnTop;
        }

        PinBtn.IsChecked      = _cfg.AlwaysOnTop;
        CollapseToggle.IsChecked = _cfg.AutoCollapse;

        AppWindow.Changed += (s, e) =>
        {
            if (!_isCollapsed)
            {
                _cfg.X = AppWindow.Position.X;
                _cfg.Y = AppWindow.Position.Y;
                _svc.Save(_cfg);
            }
        };

        Activated += (_, args) =>
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated
                && _cfg.AutoCollapse && !_isCollapsed)
                Collapse();
            else if (args.WindowActivationState != WindowActivationState.Deactivated
                && _isCollapsed)
                Expand();
        };

        Closed += (_, _) => _svc.Save(_cfg);

        RootGrid.Loaded += (_, _) => RebuildButtons();
    }

    // ── build toolbar buttons ─────────────────────────────────────────────────

    private void RebuildButtons()
    {
        ButtonsPanel.Children.Clear();
        foreach (var item in _cfg.Items)
            ButtonsPanel.Children.Add(MakeButton(item));
    }

    private Button MakeButton(ToolbarItem item)
    {
        var content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing     = 6,
        };

        if (!string.IsNullOrEmpty(item.Glyph))
            content.Children.Add(new FontIcon { Glyph = item.Glyph, FontSize = 14, VerticalAlignment = VerticalAlignment.Center });

        content.Children.Add(new TextBlock
        {
            Text = item.Label,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
        });

        var btn = new Button
        {
            Content = content,
            Height  = 36,
            Padding = new Thickness(10, 0, 10, 0),
            Tag     = item,
        };
        btn.Click += ItemBtn_Click;

        var menu = new MenuFlyout();
        var removeItem = new MenuFlyoutItem { Text = "Quitar de la barra" };
        removeItem.Click += (_, _) => RemoveItem(item);
        menu.Items.Add(removeItem);
        btn.ContextFlyout = menu;

        return btn;
    }

    private async void ItemBtn_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not ToolbarItem item) return;

        if (item.Type == ToolbarItemType.Macro)
        {
            var macro = _macros.Load().FirstOrDefault(m => m.Id == item.MacroId);
            if (macro == null) return;
            await _macros.RunAsync(macro);
        }
        else if (item.Type == ToolbarItemType.App)
        {
            try { Process.Start(new ProcessStartInfo(item.ExePath) { UseShellExecute = true }); }
            catch { }
        }
    }

    private void RemoveItem(ToolbarItem item)
    {
        _cfg.Items.Remove(item);
        _svc.Save(_cfg);
        RebuildButtons();
    }

    // ── Add button dialog ─────────────────────────────────────────────────────

    private async void AddBtn_Click(object sender, RoutedEventArgs e)
    {
        // Main window must be visible for the dialog to appear in it
        var mw = App.MainWindowInstance;
        if (mw != null)
        {
            mw.AppWindow.Show();
            mw.Activate();
        }

        var dlg = BuildAddDialog();
        dlg.XamlRoot = mw?.Content?.XamlRoot ?? Content.XamlRoot;
        if (dlg.XamlRoot == null) return;

        var result = await dlg.ShowAsync();
        if (result == ContentDialogResult.Primary)
            RebuildButtons();
    }

    private ContentDialog BuildAddDialog()
    {
        var macroList = _macros.Load();

        // Type selector
        var typeCombo = new ComboBox { Margin = new Thickness(0, 0, 0, 12) };
        typeCombo.Items.Add("Macro del sistema");
        typeCombo.Items.Add("Aplicación (.exe)");
        typeCombo.SelectedIndex = 0;

        // Macro picker
        var macroCombo = new ComboBox { PlaceholderText = "Selecciona una macro", Width = 320 };
        foreach (var m in macroList)
            macroCombo.Items.Add(m.Name);
        if (macroList.Count > 0) macroCombo.SelectedIndex = 0;

        // App path row
        var exeBox = new TextBox { PlaceholderText = "Ruta al .exe…", Width = 240 };
        var browseBtn = new Button { Content = "Examinar…", Margin = new Thickness(8, 0, 0, 0) };
        var appRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children    = { exeBox, browseBtn },
            Visibility  = Visibility.Collapsed,
        };

        var labelBox = new TextBox
        {
            PlaceholderText = "Etiqueta (opcional, se auto-rellena)",
            Width  = 320,
            Margin = new Thickness(0, 8, 0, 0),
        };

        browseBtn.Click += async (_, _) =>
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(".exe");
            WinRT.Interop.InitializeWithWindow.Initialize(
                picker,
                WinRT.Interop.WindowNative.GetWindowHandle(this));
            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                exeBox.Text = file.Path;
                if (string.IsNullOrEmpty(labelBox.Text))
                    labelBox.Text = Path.GetFileNameWithoutExtension(file.Path);
            }
        };

        typeCombo.SelectionChanged += (_, _) =>
        {
            bool isApp = typeCombo.SelectedIndex == 1;
            macroCombo.Visibility = isApp ? Visibility.Collapsed : Visibility.Visible;
            appRow.Visibility     = isApp ? Visibility.Visible   : Visibility.Collapsed;
        };

        var panel = new StackPanel { Spacing = 4, Width = 340 };
        panel.Children.Add(new TextBlock { Text = "Tipo", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        panel.Children.Add(typeCombo);
        panel.Children.Add(macroCombo);
        panel.Children.Add(appRow);
        panel.Children.Add(labelBox);

        var dlg = new ContentDialog
        {
            Title             = "Agregar botón",
            Content           = panel,
            PrimaryButtonText = "Agregar",
            CloseButtonText   = "Cancelar",
        };

        dlg.PrimaryButtonClick += (d, _) =>
        {
            bool isApp = typeCombo.SelectedIndex == 1;
            if (isApp)
            {
                if (string.IsNullOrWhiteSpace(exeBox.Text)) { d.Hide(); return; }
                var item = new ToolbarItem
                {
                    Type    = ToolbarItemType.App,
                    ExePath = exeBox.Text.Trim(),
                    Label   = string.IsNullOrWhiteSpace(labelBox.Text)
                                  ? Path.GetFileNameWithoutExtension(exeBox.Text)
                                  : labelBox.Text.Trim(),
                    Glyph   = "",  // executable icon
                };
                _cfg.Items.Add(item);
            }
            else
            {
                int idx = macroCombo.SelectedIndex;
                if (idx < 0 || idx >= macroList.Count) { d.Hide(); return; }
                var macro = macroList[idx];
                var item = new ToolbarItem
                {
                    Type    = ToolbarItemType.Macro,
                    MacroId = macro.Id,
                    Label   = string.IsNullOrWhiteSpace(labelBox.Text) ? macro.Name : labelBox.Text.Trim(),
                    Glyph   = macro.Glyph,
                };
                _cfg.Items.Add(item);
            }
            _svc.Save(_cfg);
        };

        return dlg;
    }

    // ── Pin / AlwaysOnTop ─────────────────────────────────────────────────────

    private void PinBtn_Checked(object sender, RoutedEventArgs e)   => SetAlwaysOnTop(true);
    private void PinBtn_Unchecked(object sender, RoutedEventArgs e) => SetAlwaysOnTop(false);

    private void SetAlwaysOnTop(bool value)
    {
        if (_presenter != null) _presenter.IsAlwaysOnTop = value;
        _cfg.AlwaysOnTop = value;
        _svc.Save(_cfg);
    }

    // ── Auto-collapse toggle ──────────────────────────────────────────────────

    private void CollapseToggle_Changed(object sender, RoutedEventArgs e)
    {
        _cfg.AutoCollapse = CollapseToggle.IsChecked == true;
        _svc.Save(_cfg);
    }

    // ── Collapse / Expand ─────────────────────────────────────────────────────

    private void Collapse()
    {
        _isCollapsed = true;
        CollapsedView.Visibility = Visibility.Visible;
        // Resize to a small square
        AppWindow.Resize(new SizeInt32(CollapsedSize, CollapsedSize));
    }

    private void Expand()
    {
        _isCollapsed = false;
        CollapsedView.Visibility = Visibility.Collapsed;
        AppWindow.Resize(new SizeInt32(Math.Max(600, ButtonsPanel.Children.Count * 140 + 160), ExpandedHeight));
    }

    private void CollapsedView_PointerEntered(object sender, PointerRoutedEventArgs e) => Expand();

    private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

}

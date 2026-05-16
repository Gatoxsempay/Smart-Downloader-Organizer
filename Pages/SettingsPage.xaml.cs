using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OrganizadorDescargas.Services;
using System;
using System.IO;
using Windows.Storage.Pickers;

namespace OrganizadorDescargas.Pages;

public sealed partial class SettingsPage : Page
{
    private bool _loading = true;

    public SettingsPage()
    {
        InitializeComponent();
        var cfg = MainWindow.Config.Get();
        FolderBox.Text = cfg.MonitoredFolder;
        AutoStartSwitch.IsOn = cfg.AutoStartMonitoring;
        OrganizeOnStartSwitch.IsOn = cfg.OrganizeOnStart;
        WindowsStartSwitch.IsOn = AutostartService.IsEnabled();

        ThemeCombo.SelectedIndex = cfg.Theme switch
        {
            "light"   => 1,
            "default" => 2,
            _ => 0,
        };
        _loading = false;
    }

    private async void ChangeFolder_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
        picker.FileTypeFilter.Add("*");

        // WinUI 3 requires setting the window handle
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder == null) return;

        MainWindow.Config.Update(cfg => cfg.MonitoredFolder = folder.Path);
        FolderBox.Text = folder.Path;

        if (MainWindow.Organizer.IsRunning)
        {
            MainWindow.Organizer.Stop();
            MainWindow.Organizer.Start();
        }
    }

    private void AutoStart_Toggled(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        MainWindow.Config.Update(cfg => cfg.AutoStartMonitoring = AutoStartSwitch.IsOn);
    }

    private void OrganizeOnStart_Toggled(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        MainWindow.Config.Update(cfg => cfg.OrganizeOnStart = OrganizeOnStartSwitch.IsOn);
    }

    private void WindowsStart_Toggled(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        AutostartService.SetEnabled(WindowsStartSwitch.IsOn);
    }

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        var theme = ThemeCombo.SelectedIndex switch
        {
            1 => "light",
            2 => "default",
            _ => "dark",
        };
        MainWindow.Config.Update(cfg => cfg.Theme = theme);
        App.MainWindowInstance?.ApplyTheme(theme);
    }

    private async void ResetStats_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new ContentDialog
        {
            Title = "Confirmar",
            Content = "¿Deseas restablecer todas las estadísticas a cero?",
            PrimaryButtonText = "Sí",
            CloseButtonText = "Cancelar",
            XamlRoot = XamlRoot,
        };
        if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;
        MainWindow.Config.Update(cfg => cfg.Stats = new AppStats());
    }

    private async void Reverse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new ContentDialog
        {
            Title = "Desorganizar archivos",
            Content = "Se moverán todos los archivos de las subcarpetas de vuelta a Descargas y se eliminarán las carpetas vacías.\n\n¿Continuar?",
            PrimaryButtonText = "Continuar",
            CloseButtonText = "Cancelar",
            XamlRoot = XamlRoot,
        };
        if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;
        var wasRunning = MainWindow.Organizer.IsRunning;
        if (wasRunning) MainWindow.Organizer.Stop();
        MainWindow.Organizer.Reverse();
        if (wasRunning) MainWindow.Organizer.Start();
    }

    private async void Uninstall_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new ContentDialog
        {
            Title = "Desinstalar",
            Content = "Se eliminarán:\n  • Configuración y registros\n  • Entrada de inicio automático con Windows\n\nEl archivo .exe deberás borrarlo tú manualmente.\n\n¿Continuar?",
            PrimaryButtonText = "Desinstalar",
            CloseButtonText = "Cancelar",
            XamlRoot = XamlRoot,
        };
        if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

        AutostartService.SetEnabled(false);
        var cfgDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OrganizadorDescargas");
        try { if (Directory.Exists(cfgDir)) Directory.Delete(cfgDir, true); } catch { }

        Application.Current.Exit();
    }
}

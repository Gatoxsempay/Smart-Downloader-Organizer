using Microsoft.UI.Xaml;
using OrganizadorDescargas.Services;
using System.IO;

namespace OrganizadorDescargas;

public partial class App : Application
{
    public static MainWindow? MainWindowInstance { get; private set; }

    private TrayService? _tray;

    public App()
    {
        InitializeComponent();
        UnhandledException += (_, e) =>
        {
            e.Handled = true;
            try
            {
                var logDir  = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OrganizadorDescargas");
                Directory.CreateDirectory(logDir);
                File.AppendAllText(
                    Path.Combine(logDir, "crash.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n{e.Exception}\n\n");
            }
            catch { }
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindowInstance = new MainWindow();

        _tray = new TrayService(Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        _tray.Start(iconPath);

        _tray.ShowRequested += ShowWindow;
        _tray.ExitRequested += ExitApp;

        MainWindowInstance.AppWindow.Closing += (s, e) =>
        {
            e.Cancel = true;
            MainWindowInstance.AppWindow.Hide();
        };

        MainWindowInstance.Activate();
    }

    private void ShowWindow()
    {
        if (MainWindowInstance == null) return;
        MainWindowInstance.AppWindow.Show();
        MainWindowInstance.Activate();
    }

    private void ExitApp()
    {
        MainWindow.Organizer.Stop();
        _tray?.Dispose();
        _tray = null;
        Application.Current.Exit();
    }
}

using Microsoft.UI.Xaml;

namespace OrganizadorDescargas;

public partial class App : Application
{
    public static MainWindow? MainWindowInstance { get; private set; }

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindowInstance = new MainWindow();
        MainWindowInstance.Closed += (s, e) =>
        {
            MainWindow.Organizer.Stop();
            Application.Current.Exit();
        };
        MainWindowInstance.Activate();
    }
}

using Microsoft.UI.Dispatching;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using WinForms = System.Windows.Forms;

namespace OrganizadorDescargas.Services;

public sealed class TrayService : IDisposable
{
    private WinForms.NotifyIcon? _icon;
    private Thread? _thread;
    private readonly DispatcherQueue _dispatcher;

    public event Action? ShowRequested;
    public event Action? ExitRequested;

    public TrayService(DispatcherQueue dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public void Start(string iconPath)
    {
        _thread = new Thread(() =>
        {
            _icon = new WinForms.NotifyIcon
            {
                Icon = File.Exists(iconPath) ? new Icon(iconPath) : SystemIcons.Application,
                Text = "Organizador de Descargas",
                Visible = true,
            };

            var menu = new WinForms.ContextMenuStrip();
            menu.Items.Add("Abrir", null, (s, e) => _dispatcher.TryEnqueue(() => ShowRequested?.Invoke()));
            menu.Items.Add(new WinForms.ToolStripSeparator());
            menu.Items.Add("Salir", null, (s, e) => _dispatcher.TryEnqueue(() => ExitRequested?.Invoke()));

            _icon.ContextMenuStrip = menu;
            _icon.DoubleClick += (s, e) => _dispatcher.TryEnqueue(() => ShowRequested?.Invoke());

            WinForms.Application.Run();
        });
        _thread.IsBackground = true;
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }

    public void Dispose()
    {
        if (_icon != null)
        {
            _icon.Visible = false;
            _icon.Dispose();
            _icon = null;
        }
        WinForms.Application.Exit();
    }
}

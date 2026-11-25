using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Hosting;

namespace ServerWatch.Agent;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly IHost _host;
    private readonly AgentWindow _window;
    private readonly NotifyIcon _trayIcon;

    public TrayApplicationContext(IHost host, AgentState state, AgentSettings settings)
    {
        _host = host;
        _window = new AgentWindow(state, settings);
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open ServerWatch Agent", null, (_, _) => _window.RestoreWindow());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit agent", null, ExitAgent);
        _trayIcon = new NotifyIcon { Icon = SystemIcons.Application, Text = "ServerWatch Agent", ContextMenuStrip = menu, Visible = true };
        _trayIcon.DoubleClick += (_, _) => _window.RestoreWindow();
        MainForm = _window;
        _window.Show();
    }

    private async void ExitAgent(object? sender, EventArgs e)
    {
        _trayIcon.Visible = false;
        _window.PermitExit();
        await _host.StopAsync();
        _host.Dispose();
        _window.Close();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
        base.Dispose(disposing);
    }
}

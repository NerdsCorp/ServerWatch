using System.Drawing;
using System.Windows.Forms;

namespace ServerWatch.Agent;

internal sealed class AgentWindow : Form
{
    private const string Banner = """
      _____                      __       __      __      __
     / ___/___  _______  _______/ /______/ /___ _/ /_____/ /
     \__ \/ _ \/ ___/ / / / ___/ __/ ___/ / __ `/ __/ __  /
    ___/ /  __/ /  / /_/ / /__/ /_/ /  / / /_/ / /_/ /_/ /
    /____/\___/_/   \__,_/\___/\__/_/  /_/\__,_/\__/\__,_/
       SERVERWATCH AGENT
    """;

    private readonly AgentState _state;
    private readonly AgentSettings _settings;
    private readonly Label _connectionLabel;
    private readonly Label _heartbeatLabel;
    private readonly ListView _processList;
    private bool _allowExit;

    public AgentWindow(AgentState state, AgentSettings settings)
    {
        _state = state;
        _settings = settings;
        Text = "ServerWatch Agent";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(720, 540);
        MinimumSize = new Size(640, 480);
        BackColor = Color.FromArgb(15, 23, 42);
        ForeColor = Color.FromArgb(226, 232, 240);
        Font = new Font("Segoe UI", 10);

        var bannerPanel = new Panel { Dock = DockStyle.Top, Height = 220, BackColor = Color.FromArgb(2, 6, 23), Padding = new Padding(24, 18, 24, 12) };
        var bannerLabel = new Label { Dock = DockStyle.Top, Height = 160, Text = Banner, ForeColor = Color.FromArgb(56, 189, 248), Font = new Font("Consolas", 13, FontStyle.Bold), UseCompatibleTextRendering = true };
        var subtitle = new Label { Dock = DockStyle.Bottom, Height = 26, Text = "SERVER UPTIME  /  RESOURCE  /  APPLICATION MONITOR", ForeColor = Color.FromArgb(148, 163, 184), Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        bannerPanel.Controls.Add(bannerLabel);
        bannerPanel.Controls.Add(subtitle);

        var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(26, 20, 26, 18) };
        _connectionLabel = new Label { Dock = DockStyle.Top, Height = 30, Font = new Font("Segoe UI Semibold", 13), Text = "Starting agent…" };
        _heartbeatLabel = new Label { Dock = DockStyle.Top, Height = 28, ForeColor = Color.FromArgb(148, 163, 184) };
        var appsLabel = new Label { Dock = DockStyle.Top, Height = 28, Text = "MONITORED APPLICATIONS", ForeColor = Color.FromArgb(148, 163, 184), Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        _processList = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = false, HeaderStyle = ColumnHeaderStyle.Nonclickable, BackColor = Color.FromArgb(30, 41, 59), ForeColor = Color.FromArgb(226, 232, 240), BorderStyle = BorderStyle.None };
        _processList.Columns.Add("Process", 380);
        _processList.Columns.Add("Status", 180);
        foreach (var name in settings.Processes)
            _processList.Items.Add(new ListViewItem(new[] { name, "Waiting for first check" }));
        if (settings.Processes.Count == 0)
            _processList.Items.Add(new ListViewItem(new[] { "No applications configured", "" }));

        content.Controls.Add(_processList);
        content.Controls.Add(appsLabel);
        content.Controls.Add(_heartbeatLabel);
        content.Controls.Add(_connectionLabel);
        Controls.Add(content);
        Controls.Add(bannerPanel);

        _state.Changed += OnStateChanged;
        FormClosing += OnFormClosing;
        Resize += OnResize;
        Shown += (_, _) => RefreshState();
    }

    public void PermitExit() => _allowExit = true;

    public void RestoreWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        ShowInTaskbar = true;
        Activate();
        RefreshState();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_allowExit || e.CloseReason is CloseReason.WindowsShutDown or CloseReason.TaskManagerClosing or CloseReason.ApplicationExitCall) return;
        e.Cancel = true;
        HideToTray();
    }

    private void OnResize(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized) HideToTray();
    }

    private void HideToTray()
    {
        Hide();
        ShowInTaskbar = false;
    }

    private void OnStateChanged()
    {
        if (IsDisposed || !IsHandleCreated) return;
        try { BeginInvoke((Action)RefreshState); } catch (InvalidOperationException) { }
    }

    private void RefreshState()
    {
        var snapshot = _state.Snapshot;
        _connectionLabel.Text = snapshot.Connected ? "●  Connected to master" : "●  " + snapshot.Message;
        _connectionLabel.ForeColor = snapshot.Connected ? Color.FromArgb(74, 222, 128) : Color.FromArgb(251, 191, 36);
        _heartbeatLabel.Text = snapshot.LastHeartbeatUtc is null
            ? $"Master: {_settings.MasterUrl}  ·  Waiting for first heartbeat"
            : $"Master: {_settings.MasterUrl}  ·  Last heartbeat: {snapshot.LastHeartbeatUtc.Value.ToLocalTime():g}";

        var statuses = snapshot.Processes.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        foreach (ListViewItem item in _processList.Items)
        {
            if (!statuses.TryGetValue(item.Text, out var status)) continue;
            item.SubItems[1].Text = status.Running ? "Running" : "Stopped";
            item.ForeColor = status.Running ? Color.FromArgb(74, 222, 128) : Color.FromArgb(248, 113, 113);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _state.Changed -= OnStateChanged;
        base.Dispose(disposing);
    }
}

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using ServerWatch.Agent;
using System.Diagnostics;
using System.Windows.Forms;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length >= 2 && args[0].Equals("--install-service", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                ServiceInstaller.Install(args[1]);
                MessageBox.Show("ServerWatch Agent was installed and started as a Windows service.", "ServerWatch Agent", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Service installation failed: {ex.Message}", "ServerWatch Agent", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return;
        }

        if (WindowsServiceHelpers.IsWindowsService())
        {
            RunService(args);
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        if (ServiceInstaller.IsInstalled())
        {
            MessageBox.Show("The ServerWatch Agent service is already registered. Stop or remove the service before running tray mode, to avoid duplicate monitoring.", "ServerWatch Agent", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var sourceDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var installDirectory = ServiceInstaller.InstallDirectory;
        if (!sourceDirectory.Equals(installDirectory, StringComparison.OrdinalIgnoreCase))
        {
            var answer = MessageBox.Show(
                $"The agent is running from:\n{sourceDirectory}\n\nInstall it as a Windows service in:\n{installDirectory}\n\nThe files and .env will be copied there. Windows will ask for administrator approval. Choose No to run in the system tray instead.",
                "Install ServerWatch Agent?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer == DialogResult.Yes)
            {
                try
                {
                    var executable = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName
                        ?? throw new InvalidOperationException("Could not locate the running agent executable.");
                    var start = new ProcessStartInfo(executable)
                    {
                        Arguments = "--install-service \"" + sourceDirectory.Replace("\"", "\\\"") + "\"",
                        WorkingDirectory = sourceDirectory,
                        UseShellExecute = true,
                        Verb = "runas"
                    };
                    using var elevatedInstaller = Process.Start(start)
                        ?? throw new InvalidOperationException("Could not start the elevated installer.");
                    return;
                }
                catch (Exception ex) when (ex is not OutOfMemoryException)
                {
                    MessageBox.Show($"Could not start the elevated installer. The agent will continue in tray mode.\n\n{ex.Message}", "ServerWatch Agent", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
        RunTrayAgent();
    }

    private static void RunService(string[] args)
    {
        var settings = AgentSettings.Load();
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddWindowsService(options => options.ServiceName = "ServerWatch Agent");
        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton<AgentState>();
        builder.Services.AddHostedService<Worker>();
        builder.Build().Run();
    }

    private static void RunTrayAgent()
    {
        var settings = AgentSettings.Load();
        var builder = Host.CreateApplicationBuilder(Array.Empty<string>());
        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton<AgentState>();
        builder.Services.AddHostedService<Worker>();
        var host = builder.Build();
        host.Start();
        using var context = new TrayApplicationContext(host, host.Services.GetRequiredService<AgentState>(), settings);
        Application.Run(context);
    }
}

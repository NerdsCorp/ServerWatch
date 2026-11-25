# ServerWatch Agent

Windows agent with both auto-start service and interactive system-tray modes. It samples system-wide CPU and physical memory and sends them to a ServerWatch master over HTTPS.

## Publish

From the repository root on a machine with the .NET 10 SDK and NuGet access:

```powershell
dotnet publish src/ServerWatch.Agent/ServerWatch.Agent.csproj -c Release -r win-x64 --self-contained true -o publish/ServerWatch.Agent
```

Copy the `.env` file issued when creating the site in the master panel into `publish/ServerWatch.Agent/.env`. Launch `ServerWatch.Agent.exe`. If it is outside `C:\Program Files\ServerWatch\Agent` and the service is not already registered, it asks whether to install. Approving the prompt triggers UAC; the elevated installer copies the publish folder and `.env` into Program Files, restricts `.env` access to LocalService, Administrators, and SYSTEM, registers the auto-start service as LocalService, and starts it. Choose **No** to run in the tray instead.

Manual service creation from an elevated prompt is also available:

```powershell
sc.exe create ServerWatchAgent binPath= '"C:\Program Files\ServerWatch\Agent\ServerWatch.Agent.exe"' start= auto obj= 'NT AUTHORITY\LocalService'
icacls 'C:\Program Files\ServerWatch\Agent\.env' /inheritance:r /grant:r 'NT AUTHORITY\LOCAL SERVICE:R' 'BUILTIN\Administrators:F' 'NT AUTHORITY\SYSTEM:F'
sc.exe start ServerWatchAgent
```

Protect the deployment directory so only administrators can modify the executable or `.env`. The LocalService account only needs read and execute access. Check the Windows Application event log for service errors. Stop/remove with `sc.exe stop ServerWatchAgent` and `sc.exe delete ServerWatchAgent`.

## Interactive tray mode

To run it as a desktop app instead, stop the Windows service and launch `ServerWatch.Agent.exe` from the signed-in user's session. The window shows an ASCII ServerWatch banner, connection state, last heartbeat, and monitored app status. Closing or minimizing the window hides it in the system tray and monitoring keeps running. Double-click the tray icon or choose **Open ServerWatch Agent** to show the window again. Choose **Exit agent** in the tray menu to stop monitoring. Start the executable again to resume.

Run either the service or the tray app on a server, not both, so it does not send duplicate heartbeats.

The agent refuses non-HTTPS remote URLs, uses normal TLS certificate validation, does not follow redirects, and never logs the key. For local development only, an HTTP URL on localhost is accepted.

## Configuration

The agent reads `.env` beside the executable:

```dotenv
MONITOR_MASTER_URL=https://monitor.example.com
MONITOR_SITE_KEY=sw_<one-time-key-from-master>
MONITOR_INTERVAL_SECONDS=30
MONITOR_PROCESSES=MyWebApp,AnotherApp
```

`MONITOR_PROCESSES` is optional and accepts up to six comma-separated Windows process names, with or without the `.exe` suffix. These names must match the applications configured for that site in the master. The agent reports whether at least one process with each name is running on every heartbeat. If you change the application's list in the master, update this setting and restart the agent.

Allowed intervals are 5–3600 seconds. If the server is unreachable, the agent retries with bounded exponential backoff. A rejected key is logged without exposing its value.

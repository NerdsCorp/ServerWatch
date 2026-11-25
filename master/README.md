# ServerWatch master

Laravel 13 + Filament 5 control panel and JSON API. SQLite stores sites, SHA-256 hashes of agent keys, heartbeats, and incident history. Agents connect to `POST /api/v1/heartbeat`.

## Run with Docker Compose

Docker Compose runs one container with PHP-FPM, Nginx, and the Laravel scheduler supervised together. SQLite data and Laravel runtime files use named Docker volumes and survive container replacement.

Run the following commands from the repository root:

```powershell
Copy-Item master/.env.docker.example master/.env
docker compose pull
```

Set `APP_URL` in `master/.env` to the public HTTPS URL. For local-only development, set `APP_URL=http://localhost:8080` and `SESSION_SECURE_COOKIE=false`. Generate a key before starting the container with `php -r 'echo "base64:".base64_encode(random_bytes(32));'`, then put the output in `APP_KEY` in `master/.env`. Keep the same key across restarts and back it up with the database; changing it invalidates sessions and encrypted Laravel data.

```powershell
docker compose up -d
docker compose exec master php artisan make:filament-user
```

Open `http://localhost:8080/admin` for local use. The host port defaults to `8080`; set `SERVERWATCH_PORT` in your shell before starting Compose to choose a different available host port (for example, `$env:SERVERWATCH_PORT = "9000"` in PowerShell, then open `http://localhost:9000/admin`). The container listens on port `8080`. Put an HTTPS reverse proxy in front of it for remote agents and point agents at the public HTTPS URL. Never run with `APP_DEBUG=true` in production. Do not publish the PHP-FPM port.

On startup, the `master` container validates the configured `APP_KEY`, creates the SQLite database file and Laravel storage/cache directories if they are missing, refreshes the package manifest, and runs outstanding migrations before supervising PHP-FPM, Nginx, and `php artisan schedule:work`. Docker restarts this container after a host reboot. Compose pulls the unified `ghcr.io/nerdscorp/serverwatch:latest` image from GitHub Container Registry and serves the web panel at `http://localhost:8080`. SQLite is stored in the explicitly named `serverwatch-data` volume at `/var/lib/serverwatch/database.sqlite`; back up that volume regularly. `docker compose down` preserves named volumes; `docker compose down -v` deletes them and all stored monitoring data.

Use a Linux container engine. On Windows 11 this can be Docker Desktop with its Linux container backend. A Windows Server host needs a Linux Docker engine/VM or a remote Linux Docker host for these Linux-based PHP and Nginx images.

To stop or update:

```powershell
docker compose pull
docker compose up -d
```

For a native PHP installation, run commands from `master/`: copy `.env.example` to `.env`, install Composer dependencies, create `database/database.sqlite`, run `php artisan key:generate` and `php artisan migrate --force`, then create an admin with `php artisan make:filament-user`. Configure an HTTPS web server and run `php artisan schedule:work` or schedule `php artisan schedule:run` once a minute.

## Monitor the scheduler

Docker Compose runs `php artisan schedule:work` under Supervisor in the master container. Outside Docker, use `php artisan schedule:work` for development or schedule `php artisan schedule:run` once per minute. `monitor:check-offline` records one incident when a site transitions from online to stale; it does not create duplicate incidents every check. The stale threshold defaults to 180 seconds and can be changed with `MONITOR_OFFLINE_AFTER_SECONDS`.

## Add a site and enroll its agent

1. Sign in at `/admin`, open **Monitored sites**, and add a site name, hostname, and up to 6 Windows application process names (for example, `MyWebApp`). Use process names without `.exe`.
2. The master creates a random `sw_...` key and stores only its SHA-256 hash in SQLite. The one-time success notice shows the client `.env` contents, including the configured app names. Copy it to the client install folder as `.env`; it will not be shown again.
3. Protect the client `.env` with Windows file permissions. If the key is lost, delete and recreate the site to issue a fresh key.

The secret belongs in the client's `.env`; the master allowlist and configured process names are stored in SQLite. The master checks each agent report against that site's configured app list, stores the latest running state, and records/notifies when an app stops or recovers. Checks happen with each heartbeat (30 seconds by default). If you edit the app list later, update `MONITOR_PROCESSES` in the client `.env` and restart the agent. Treat the one-time notice and client config as secrets.

## Notifications

Set `MONITOR_NOTIFICATION_WEBHOOK_URL` in the master's `.env` to a private Slack-compatible or generic JSON webhook endpoint. Offline, app-stopped, and app-recovered incidents are sent as JSON with `event`, `site`, `hostname`, `message`, `occurred_at`, and a Slack-compatible `text` field. The Filament **Application checks** page shows the latest state for each app. The `NotificationConnector` interface is the extension point for email, Teams, or other providers. Webhook failures are logged and do not discard the incident row.

## Reports

Filament's **Resource reports** table shows stored samples and supports site filtering. Export CSV for a time window with:

```powershell
php artisan monitor:export-metrics --from="2026-11-01" --to="2026-11-24 23:59:59" --output="metrics.csv"
```

Use `--site=ID` to limit it to one site. Without `--output`, CSV is written to standard output. The master does not currently expire metric or incident history; plan SQLite backups and disk capacity for the sample interval and number of agents you enroll.

## Production security checklist

- HTTPS with a valid certificate; do not disable TLS certificate validation.
- Keep API keys and application secrets out of source control and logs. Rotate a site key by removing/recreating its site and replacing the client `.env`.
- Run PHP under a dedicated low-privilege account. Keep SQLite outside the public web root and back it up securely.
- Limit `/admin` access at the proxy/firewall where practical and enable MFA at the identity/proxy layer.
- Restrict webhook egress and set the webhook URL through trusted server configuration.

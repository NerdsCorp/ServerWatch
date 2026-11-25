# ServerWatch

ServerWatch has a Dockerized Laravel + Filament master in [`master/`](master/README.md) and a Windows Server reporting agent in [`src/ServerWatch.Agent/`](src/ServerWatch.Agent/README.md). The master stores site status, heartbeat samples, and incidents in SQLite. The agent sends CPU, physical memory, and up to six configured application process statuses every 30 seconds by default.

See the linked READMEs for installation and production setup. The master runs in Docker Compose; the agent needs the .NET 10 SDK and NuGet access to publish.

GitHub Actions builds the self-contained Windows agent and validates the unified Linux container image when a pull request is opened or updated. Pushes to `main` publish `ghcr.io/nerdscorp/serverwatch:main` and a commit-SHA tag to GitHub Container Registry (GHCR). Releases publish `latest`, the release tag, and a commit-SHA tag. Manual runs publish a commit-SHA tag and also publish `main` or `latest` when run from those refs. The GHCR image supports `linux/amd64` and `linux/arm64` and contains PHP-FPM, Nginx, and the Laravel scheduler. Each release also includes `ServerWatch-Agent-win-x64.zip`; pull request and manual builds keep the `ServerWatch-Agent-win-x64` workflow artifact.

Sign in to GHCR if the package is private, then download and start the single master container with `docker compose pull` followed by `docker compose up -d`. For public pulls, set the package visibility to public in GitHub Packages.

# ServerWatch

ServerWatch has a Dockerized Laravel + Filament master in [`master/`](master/README.md) and a Windows Server reporting agent in [`src/ServerWatch.Agent/`](src/ServerWatch.Agent/README.md). The master stores site status, heartbeat samples, and incidents in SQLite. The agent sends CPU, physical memory, and up to six configured application process statuses every 30 seconds by default.

See the linked READMEs for installation and production setup. The master runs in Docker Compose; the agent needs the .NET 10 SDK and NuGet access to publish.

GitHub Actions builds the self-contained Windows agent and validates the master container images when a pull request is opened or updated. On a release or manual run, it publishes both Linux images to GitHub Container Registry (GHCR): `ghcr.io/nerdscorp/serverwatch-master` and `ghcr.io/nerdscorp/serverwatch-web`. Releases receive `latest`, the release tag, and a commit SHA tag. The agent is uploaded as the `ServerWatch-Agent-win-x64` workflow artifact.

Sign in to GHCR if the packages are private, then download and start the master with `docker compose pull` followed by `docker compose up -d`. For a public deployment, set both packages to public in GitHub Packages; Docker can then pull them without credentials.

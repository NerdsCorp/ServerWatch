# ServerWatch

ServerWatch has a Dockerized Laravel + Filament master in [`master/`](master/README.md) and a Windows Server reporting agent in [`src/ServerWatch.Agent/`](src/ServerWatch.Agent/README.md). The master stores site status, heartbeat samples, and incidents in SQLite. The agent sends CPU, physical memory, and up to six configured application process statuses every 30 seconds by default.

See the linked READMEs for installation and production setup. The master runs in Docker Compose; the agent needs the .NET 10 SDK and NuGet access to publish.

GitHub Actions builds the self-contained Windows agent and both master container images when a pull request is opened or updated, when a release is published, or when manually started. Download the `ServerWatch-Agent-win-x64` artifact from a successful workflow run. Container images are built for validation but are not pushed to a registry.

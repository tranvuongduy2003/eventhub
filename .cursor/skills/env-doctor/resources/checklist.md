# Env Doctor — Extended Checklist

Use when the quick workflow in `SKILL.md` is insufficient or failures are intermittent.

## Windows-specific

- Docker Desktop: WSL 2 backend enabled; integration with default distro on
- Hyper-V / virtualization enabled in BIOS if Docker reports it disabled
- Long paths: if restore or `node_modules` fails, enable Win32 long paths or use a shorter clone path
- Dev cert trust: may require elevated PowerShell for `dotnet dev-certs https --trust`
- Firewall: block only if Aspire dashboard or published ports are explicitly denied

## macOS-specific

- Docker Desktop allocated enough memory (≥ 4 GB recommended for Postgres + Redis)
- Rosetta: only relevant if mixing arm64/x64 Node or .NET installs

## Linux-specific

- User in `docker` group or rootless Docker configured
- `DOCKER_HOST` not pointing at a dead socket

## .NET / Aspire

```bash
dotnet --info
dotnet workload list
dotnet build src/AppHost/EventHub.AppHost.csproj
```

Watch for:

- Missing Aspire workload → `dotnet workload install aspire` (confirm with user)
- Multiple SDKs: build may pick wrong SDK without `global.json`
- NU1100 / package not found → feed auth or offline mode

## Node / frontend (`web/`)

```bash
node --version   # LTS
yarn --version
```

- Lockfile: `web/yarn.lock` — prefer `yarn --cwd web install --frozen-lockfile`
- `VITE_*` vars: normally set by Aspire at runtime; local `.env.local` overrides only for standalone frontend dev
- EBUSY / EPERM on Windows: close dev server and IDE file watchers before deleting `node_modules`

## Docker resources

```bash
docker ps
docker compose ls    # should be empty for this project (no Compose)
docker system df
```

- Disk full → prune unused images/containers (ask before `docker system prune -a`)
- Port in use: `netstat` / `Get-NetTCPConnection` for 5432, 6379, and Aspire-assigned ports
- Postgres volume corruption: last resort — remove named volume and restart AppHost (data loss in dev)

## Network and connectivity

- Corporate VPN/proxy breaking NuGet, Yarn registry, or Docker pulls
- `localhost` vs `127.0.0.1` mismatches in frontend API URL
- IPv6-only issues: try forcing IPv4 for Docker DNS

## Common error → action map

| Error pattern | Action |
|---------------|--------|
| `Cannot connect to Docker daemon` | Start Docker Desktop / daemon |
| `SDK not found` / `NETSDK1045` | Install required .NET SDK version |
| `password authentication failed` (Postgres) | Stale volume or wrong connection string; restart Aspire resources |
| `Connection refused` (Redis) | Redis container not healthy; check dashboard |
| `NU1301` / Yarn registry timeout | Network/proxy; retry or mirror |
| `certificate is not trusted` | Trust dev HTTPS cert |
| Aspire resource "Unhealthy" | Open resource logs in dashboard; fix dependency order |

## Escalation

Stop and ask the user when:

- Fixing requires admin rights you cannot use
- Data loss risk (volume wipe, production credentials)
- Hardware virtualization cannot be enabled
- Symptom is clearly application code, not tooling

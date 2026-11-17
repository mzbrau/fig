---
sidebar_position: 14
---

# FAQ's

## How do I run Fig locally?

The fastest path is the [fig-quick-start](https://github.com/mzbrau/fig-quick-start) repository, or [Aspire](./guides/9-aspire-integration.md). For a SQL Server deployment, copy `.env.example` to `.env` and run `docker compose up` from the Fig repository root. Fig Web is then at `http://localhost:7148` (user `admin` / password `admin`) and the API at `http://localhost:7281`.

## How to build and run containers locally?

From the `src` directory:

```
docker build -f api/Fig.Api/Dockerfile -t fig.api .
docker run -p 7281:8080 -it fig.api
```

```
docker build -f web/Fig.Web/Dockerfile -t fig.web .
docker run -p 7148:80 -e FIG_API_URI=http://localhost:7281 fig.web
```

Open `http://localhost:7148`. The Web image substitutes `FIG_API_URI` into `appsettings.json` at startup. Prefer published images (`mzbrau/fig-api`, `mzbrau/fig-web`) or compose for anything beyond a smoke test.

The API container listens on port **8080** (mapped to **7281** in compose). Fig Web (nginx) listens on port **80** (mapped to **7148**). The environment variable is **`FIG_API_URI`**, not `FIG_API_ADDRESS`.

## Can I run this on an Apple Silicon (M1/M2/M3/M4) Mac?

Yes. Containers work on Apple Silicon, and building the solution locally also works without extra SQLite setup.

Fig.Api uses **System.Data.SQLite 2.x** with native SQLite from the **SourceGear.sqlite3** NuGet package, which includes `osx-arm64` binaries. No hand-built interop libraries are required. See [Database](./database.md).

```
dotnet build
dotnet run --project src/api/Fig.Api
```

Or use the Aspire AppHost under `src/hosting/Fig.AppHost`.

## How do I install Fig as a Windows service?

[`scripts/Install-Fig.ps1`](https://github.com/mzbrau/fig/blob/main/scripts/Install-Fig.ps1) downloads the latest GitHub release zip and installs Fig API (Windows service) and Fig Web. Review the script before running it; it is intended for Windows hosts, not containers.

Enable IIS, IIS Management Scripts and Tools, and the IIS URL Rewrite module before running the installer as administrator. PowerShell 7 loads IISAdministration through Windows PowerShell compatibility; if that fails, run the installer in Windows PowerShell 5.1.

Database setup defaults to `Server=localhost;Initial Catalog=master;Integrated Security=true;TrustServerCertificate=true`. The account must be able to create databases and logins. Setup creates the database before configuring its user and stops the installation if a SQL batch fails. For production, provide a connection string with certificate validation enabled and a trusted SQL Server certificate.

The installer can be rerun after an interruption. It reuses and checks the existing `fig_login` credentials, prompting for the password if they cannot be recovered, and leaves an existing IIS site, service, and machine `FIG_API_URI` in place. It ensures the API's `WebClientAddresses` allows both `http://localhost:<webPort>` and `http://<computer-name>:<webPort>`, preserving other configured origins. After changing this list manually, restart the `Fig.Api` Windows service to apply the CORS policy. Newly generated credentials are temporarily stored encrypted for the Windows user running the installer until the API settings are saved. Use the same Windows account to resume, or provide the password when prompted.

Each run downloads the latest release and replaces the installed files, preserving existing `appsettings*.json` and `web.config` files. Previous installation directories are retained alongside them as `*.fig-backup-<id>`; restore other custom files from those backups if needed. Backups accumulate and consume disk space. Existing Web settings are not changed; verify the API address if installation stopped before those settings were configured. Updating requires internet access. If an update fails after the API service is stopped, resolve the error and rerun the installer. Switching from an existing SQLite configuration requires confirmation and does not migrate its data.

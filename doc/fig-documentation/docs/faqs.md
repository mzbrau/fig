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

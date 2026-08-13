---
sidebar_position: 11
---

# FAQ's

## How to build and run containers locally?

1. Open the terminal
2. Set the directory to src
3. Build the api
```
docker build -f api/Fig.Api/Dockerfile -t fig.api .
```
4. Start the api
```
docker run -p 5000:80 -it fig.api
```
5. Build the web
```
docker build -f web/Fig.Web/Dockerfile -t fig.web .
```
6. Start the web
```
docker run -p 8080:80 -e FIG_API_ADDRESS=https://localhost:5000 fig.web
```
7. Open a web browser and navigate to https://localhost:8080


## How to export a container image

https://stackoverflow.com/a/46526598
```
docker export $(docker ps -lq) -o fig.web.tar
```



## Can I run this on an Apple Silicon (M1/M2/M3/M4) Mac?

Yes. Containers work on Apple Silicon, and building the solution locally also works without extra SQLite setup.

Fig.Api uses **System.Data.SQLite 2.x** with native SQLite from the **SourceGear.sqlite3** NuGet package, which includes `osx-arm64` binaries (`libe_sqlite3.dylib`). No hand-built interop libraries or copies under `/usr/local/lib` are required.

```
dotnet build
dotnet run --project src/api/Fig.Api
```

Or use the Aspire AppHost under `src/hosting/Fig.AppHost`.

# References

https://daniel-vetter86.medium.com/building-a-ci-cd-pipeline-with-asp-net-core-github-actions-docker-and-a-linux-server-3fc5271ebbe4

https://chrissainty.com/containerising-blazor-applications-with-docker-containerising-a-blazor-webassembly-app/

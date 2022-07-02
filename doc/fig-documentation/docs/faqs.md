---
sidebar_position: 7
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

# References

https://daniel-vetter86.medium.com/building-a-ci-cd-pipeline-with-asp-net-core-github-actions-docker-and-a-linux-server-3fc5271ebbe4

https://chrissainty.com/containerising-blazor-applications-with-docker-containerising-a-blazor-webassembly-app/

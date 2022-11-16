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



## Can I run this on an Apple Silicon (M1/M2) Mac?

Yes, the easiest way to run fig is by using containers, these run on Apple silicon as well as any other processor. However, if you want to build the solution on an M1/M2 Mac, follow these steps:

1. Clone the code
2. Copy `libSQLite.Interop.dll` from the `external` folder to  `/usr/local/lib` (it may need to be created)
3. You should be up and running.

If you want to do everything from scratch, follow the steps below:

1. Clone the code

2. From the [SQLite download page](https://system.data.sqlite.org/index.html/doc/trunk/www/downloads.wiki), download the sql lite source code. At time of writing, this was [sqlite-netFx-source-1.0.116.0.zip](https://system.data.sqlite.org/downloads/1.0.116.0/sqlite-netFx-source-1.0.116.0.zip)

3. Unzip

4. Open /Setup/compile-interop-assembly-release.sh in a text editor.

5. Change the architecture from `x86_64` to `arm64` (or `arm64e` if you are running macOS earlier than Ventura). It should look like this

6. ```sh
   if [[ "$OSTYPE" == "darwin"* ]]; then
     libname=libSQLite.Interop.dylib
     # NOTE: No longer works in 10.14+
     # gccflags="-arch i386 -arch x86_64"
     gccflags="-arch arm64"
   else
     libname=libSQLite.Interop.so
     gccflags=""
   fi
   ```

7. In the terminal Execute the following

8. ```
   cd Setup
   sh compile-interop-assembly-release.sh
   ```

9. It will create this file: `/bin/2013/Release/bin/SQLite.Interop.dll` 

10. Create the directory `/usr/local/lib` (if it didn't already exist) and copy `SQLite.Interop.dll` to that directory.

11. Rename it to `libSQLite.Interop.dll` 

12. In the terminal, return to the base directory and build the project using the following command:

13. ```
    dotnet build -c Release SQLite.NET.NetStandard20.sln
    ```

14. Add a reference the file which now has been created at `/bin/NetStandard20/ReleaseNetStandard20/bin/netstandard2.0/System.Data.SQLite.dll` to the Fig.Api project (you can move it first to a more approproiate location)

# References

https://daniel-vetter86.medium.com/building-a-ci-cd-pipeline-with-asp-net-core-github-actions-docker-and-a-linux-server-3fc5271ebbe4

https://chrissainty.com/containerising-blazor-applications-with-docker-containerising-a-blazor-webassembly-app/

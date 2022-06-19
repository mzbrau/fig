# From source directory (src)
# docker build -f web/Fig.Web/Dockerfile -t fig.web .
# TODO: Not currently working.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["web/Fig.Web/Fig.Web.csproj", "web/Fig.Web/"]
COPY ["common/Fig.Contracts/Fig.Contracts.csproj", "common/Fig.Contracts/"]
RUN dotnet restore "web/Fig.Web/Fig.Web.csproj"
COPY . .
WORKDIR "/src/web/Fig.Web"
RUN dotnet build "Fig.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Fig.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Fig.Web.dll"]
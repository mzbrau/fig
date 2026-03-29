# Run from src directory
# docker build -f api/Fig.Api/Dockerfile -t fig.api .
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

RUN apt-get update \
    && apt-get install -y curl

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG VERSION=0.0.0
WORKDIR /src

# Copy project files for restore layer caching
COPY ["api/Fig.Api/Fig.Api.csproj", "api/Fig.Api/"]
COPY ["api/Fig.Datalayer/Fig.Datalayer.csproj", "api/Fig.Datalayer/"]
COPY ["common/Fig.Common/Fig.Common.csproj", "common/Fig.Common/"]
COPY ["common/Fig.Common.NetStandard/Fig.Common.NetStandard.csproj", "common/Fig.Common.NetStandard/"]
COPY ["common/Fig.Contracts/Fig.Contracts.csproj", "common/Fig.Contracts/"]
COPY ["common/Fig.WebHooks.Contracts/Fig.WebHooks.Contracts.csproj", "common/Fig.WebHooks.Contracts/"]
COPY ["hosting/Fig.ServiceDefaults/Fig.ServiceDefaults.csproj", "hosting/Fig.ServiceDefaults/"]
COPY ["client/Fig.Client.Abstractions/Fig.Client.Abstractions.csproj", "client/Fig.Client.Abstractions/"]
COPY ["Directory.Build.props", "Directory.Build.props"]

RUN dotnet restore "api/Fig.Api/Fig.Api.csproj"

# Copy source code
COPY . .

RUN dotnet build "api/Fig.Api/Fig.Api.csproj" -c Release -o /app/build /p:Version=${VERSION}

FROM build AS publish
ARG VERSION=0.0.0
RUN dotnet publish "api/Fig.Api/Fig.Api.csproj" -c Release -o /app/publish /p:Version=${VERSION}

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Fig.Api.dll"]
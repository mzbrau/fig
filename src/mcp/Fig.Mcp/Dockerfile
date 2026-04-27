# Run from src directory
# docker build -f mcp/Fig.Mcp/Dockerfile -t fig.mcp .
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

RUN apt-get update \
    && apt-get install -y curl \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG VERSION=0.0.0
WORKDIR /src

# Copy project files for restore layer caching
COPY ["mcp/Fig.Mcp/Fig.Mcp.csproj", "mcp/Fig.Mcp/"]
COPY ["common/Fig.Common.NetStandard/Fig.Common.NetStandard.csproj", "common/Fig.Common.NetStandard/"]
COPY ["common/Fig.Contracts/Fig.Contracts.csproj", "common/Fig.Contracts/"]
COPY ["client/Fig.Client.Abstractions/Fig.Client.Abstractions.csproj", "client/Fig.Client.Abstractions/"]
COPY ["Directory.Build.props", "Directory.Build.props"]

RUN dotnet restore "mcp/Fig.Mcp/Fig.Mcp.csproj"

# Copy source code
COPY . .

RUN dotnet build "mcp/Fig.Mcp/Fig.Mcp.csproj" -c Release -o /app/build /p:MinVerVersionOverride=${VERSION}

FROM build AS publish
ARG VERSION=0.0.0
RUN dotnet publish "mcp/Fig.Mcp/Fig.Mcp.csproj" -c Release -o /app/publish /p:MinVerVersionOverride=${VERSION}

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Fig.Mcp.dll"]

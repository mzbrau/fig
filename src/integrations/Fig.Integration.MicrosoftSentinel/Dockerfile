# Run from src directory
# docker build -f integrations/Fig.Integration.MicrosoftSentinel/Dockerfile -t fig.integration.microsoftsentinel .
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

RUN apt-get update \
    && apt-get install -y curl

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["integrations/Fig.Integration.MicrosoftSentinel/Fig.Integration.MicrosoftSentinel.csproj", "integrations/Fig.Integration.MicrosoftSentinel/"]
COPY ["client/Fig.Client/Fig.Client.csproj", "client/Fig.Client/"]
COPY ["common/Fig.Common.NetStandard/Fig.Common.NetStandard.csproj", "common/Fig.Common.NetStandard/"]
COPY ["common/Fig.Common/Fig.Common.csproj", "common/Fig.Common/"]
COPY ["common/Fig.WebHooks.Contracts/Fig.WebHooks.Contracts.csproj", "common/Fig.WebHooks.Contracts/"]
COPY ["hosting/Fig.ServiceDefaults/Fig.ServiceDefaults.csproj", "hosting/Fig.ServiceDefaults/"]
COPY ["common/Fig.Contracts/Fig.Contracts.csproj", "common/Fig.Contracts/"]
COPY ["client/Fig.Client.Contracts/Fig.Client.Contracts.csproj", "client/Fig.Client.Contracts/"]
COPY ["Directory.Build.props", "Directory.Build.props"]

# Restore dependencies
RUN dotnet restore "integrations/Fig.Integration.MicrosoftSentinel/Fig.Integration.MicrosoftSentinel.csproj"

# Copy source code
COPY . .

# Build the application
RUN dotnet build "integrations/Fig.Integration.MicrosoftSentinel/Fig.Integration.MicrosoftSentinel.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "integrations/Fig.Integration.MicrosoftSentinel/Fig.Integration.MicrosoftSentinel.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Fig.Integration.MicrosoftSentinel.dll"]
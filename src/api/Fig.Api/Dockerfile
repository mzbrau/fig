# Run from src directory
# docker build -f api/Fig.Api/Dockerfile -t fig.api .
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["api/Fig.Api/Fig.Api.csproj", "api/Fig.Api/"]
COPY ["common/Fig.Contracts/Fig.Contracts.csproj", "common/Fig.Contracts/"]
COPY ["api/Fig.Api.SettingVerification/Fig.Api.SettingVerification.csproj", "api/Fig.Api.SettingVerification/"]
COPY ["api/Fig.Api.SettingVerification.Sdk/Fig.Api.SettingVerification.Sdk.csproj", "api/Fig.Api.SettingVerification.Sdk/"]
COPY ["api/Fig.Datalayer/Fig.Datalayer.csproj", "api/Fig.Datalayer/"]
COPY ["common/Fig.Common/Fig.Common.csproj", "common/Fig.Common/"]
RUN dotnet restore "api/Fig.Api/Fig.Api.csproj"
COPY . .
WORKDIR "/src/api/Fig.Api"
RUN dotnet build "Fig.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Fig.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Fig.Api.dll"]

# From source directory (src)
# docker build -f web/Fig.Web/Dockerfile -t fig.web .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG VERSION=0.0.0
WORKDIR /src

# Copy project files for restore layer caching
COPY ["web/Fig.Web/Fig.Web.csproj", "web/Fig.Web/"]
COPY ["common/Fig.Common/Fig.Common.csproj", "common/Fig.Common/"]
COPY ["common/Fig.Common.NetStandard/Fig.Common.NetStandard.csproj", "common/Fig.Common.NetStandard/"]
COPY ["common/Fig.Contracts/Fig.Contracts.csproj", "common/Fig.Contracts/"]
COPY ["client/Fig.Client.Abstractions/Fig.Client.Abstractions.csproj", "client/Fig.Client.Abstractions/"]
COPY ["Directory.Build.props", "Directory.Build.props"]

RUN dotnet restore "web/Fig.Web/Fig.Web.csproj"

# Copy source code
COPY . .

RUN dotnet build "web/Fig.Web/Fig.Web.csproj" -c Release -o /app/build /p:Version=${VERSION}

FROM build AS publish
ARG VERSION=0.0.0
RUN dotnet publish "web/Fig.Web/Fig.Web.csproj" -c Release -o /app/publish /p:Version=${VERSION}

FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html
COPY --from=publish /app/publish/wwwroot .
COPY ["web/Fig.Web/nginx.conf", "/etc/nginx/nginx.conf"]
COPY ["web/Fig.Web/ssl.conf.template", "/etc/templates/ssl.conf.template"]
COPY ["web/Fig.Web/ssl_ca.conf.template", "/etc/templates/ssl_ca.conf.template"]
COPY ["web/Fig.Web/entrypoint.sh", "/entrypoint.sh"]
RUN chmod +x /entrypoint.sh
ENTRYPOINT ["/entrypoint.sh"]
﻿# From source directory (src)
# docker build -f web/Fig.Web/Dockerfile -t fig.web .

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

RUN apt update
RUN apt install git
WORKDIR /data
RUN git clone https://github.com/mzbrau/fig.git /data/app
WORKDIR /data/app/src

RUN dotnet restore "web/Fig.Web/Fig.Web.csproj"
RUN dotnet build "web/Fig.Web/Fig.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "web/Fig.Web/Fig.Web.csproj" -c Release -o /app/publish

FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html
COPY --from=publish /app/publish/wwwroot .
COPY ["web/Fig.Web/nginx.conf", "/etc/nginx/nginx.conf"]
COPY ["web/Fig.Web/ssl.conf.template", "/etc/templates/ssl.conf.template"]
COPY ["web/Fig.Web/ssl_ca.conf.template", "/etc/templates/ssl_ca.conf.template"]
COPY ["web/Fig.Web/entrypoint.sh", "/entrypoint.sh"]
RUN chmod +x /entrypoint.sh
ENTRYPOINT [ "/entrypoint.sh"]
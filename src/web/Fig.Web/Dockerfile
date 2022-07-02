# From source directory (src)
# docker build -f web/Fig.Web/Dockerfile -t fig.web .

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

FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html
COPY --from=publish /app/publish/wwwroot .
COPY ["web/Fig.Web/nginx.conf", "/etc/nginx/nginx.conf"]
RUN sed -i "s|https://localhost:7281|http://localhost:5000|g" /usr/share/nginx/html/appsettings.json
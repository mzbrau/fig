# Run from src directory
# docker build -f api/Fig.Api/Dockerfile -t fig.api .
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

RUN apt update
RUN apt install git
WORKDIR /data
RUN git clone https://github.com/mzbrau/fig.git /data/app
WORKDIR /data/app/src

RUN dotnet restore "api/Fig.Api/Fig.Api.csproj"
RUN dotnet build "api/Fig.Api/Fig.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "api/Fig.Api/Fig.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Fig.Api.dll"]
---
sidebar_position: 5
---

# Configuring TLS

Fig is able to run both its api and web instances with TLS enabled, provided the required environment variables and pem encoded certificate files are made available. This is aimed at use within docker containers, but equivalent configuration on host environments such as windows will also work for the api, but not the web instance.

## Fig Api

To enable this within the Fig-Api docker container:

1. Define the kestral environment variables
2. Mount the pem encoded certificate and key at the paths defined in the corresponding path variables
3. Update the port mapping and health check to match the https port that Fig-Api is now listening at. Below is an example docker-compose file snippet representing the required configuration.

```yaml
fig-api:
  image: mzbrau/fig-api:latest
  container_name: fig-api
  ports:
    - "7281:7148"
  depends_on:
    fig-setup:
      condition: service_completed_successfully
  environment:
    - ApiSettings:DbConnectionString=Server=${fqdn};User Id=${FIG_USER_NAME};Password=${FIG_DB_PWD};Initial Catalog=${FIG_DB_NAME}
    - ASPNETCORE_Kestrel__Endpoints__Https__Url=https://*:7281
    - ASPNETCORE_Kestrel__Certificates__Default__Path=/usr/bin/certs/fig_https.pfx
    - ASPNETCORE_Kestrel__Certificates__Default__Password=${SECRET_PFX_PASSWORD}
    - SSL_CERT_FILE=/usr/bin/certs/ca.crt
  volumes:
    - ./fig.pem:/usr/bin/certs/fig.pem
    - ./fig.key:/usr/bin/certs/fig.key
  healthcheck:
      test: ["CMD", "curl", "-f", "https://localhost:7281/_health", "--cacert", "/usr/bin/certs/ca.crt"]
      start_period: 30s
      interval: 5s
      timeout: 10s
      retries: 3
    volumes:
      - /etc/hosts:/etc/hosts:ro
      - /home/docker/mounts/secrets/fig_api:/usr/bin/certs  
```

## Fig Web

To enable this within the Fig-Web docker container:

1. Define the SSL_CERT_PATH, SSL_KEY_PATH, FIG_API_SSL_PORT, and optionally SSL_TRUSTED_CERT_PATH environment variables:
   - SSL_CERT_PATH: The path to the full chain pem encoded certificate
   - SSL_KEY_PATH: The path to the pem encoded private key
   - FIG_API_SSL_PORT: The port that the web instance will run on
   - SSL_TRUSTED_CERT_PATH: The path to a pem encoded trusted certificate.

2. Mount the pem encoded certificate, key, and optionally trusted certificate at the paths defined in the corresponding path variables
3. Update the port mapping and health check to match the https port that Fig-Web is now listening at. Below is an example docker-compose file snippet representing the required configuration.

```yaml
fig-web:
  image: mzbrau/fig-web:latest
  container_name: fig-web
  ports:
    - "7148:443"
  depends_on:
    fig-api:
      condition: service_healthy
  environment:
    - FIG_API_URI=https://localhost:7281
    - SSL_CERT_PATH=/usr/local/nginx/certs/fig.pem
    - SSL_KEY_PATH=/usr/local/nginx/certs/fig.key
    - SSL_TRUSTED_CERT_PATH=/usr/local/nginx/certs/ca.pem
    - FIG_WEB_SSL_PORT=443
  volumes:
    - ./fig.key:/usr/local/nginx/certs/fig.key
    - ./fig.pem:/usr/local/nginx/certs/fig.pem
    - ./ca.pem:/usr/local/nginx/certs/ca.pem
  healthcheck:
    test: ["CMD", "curl", "-f", "https://localhost:443"]
    start_period: 30s      
    interval: 5s
    timeout: 10s
    retries: 3
```

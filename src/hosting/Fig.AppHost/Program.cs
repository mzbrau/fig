using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var useKeycloak = bool.TryParse(builder.Configuration["UseKeycloak"], out var parsedUseKeycloak) &&
                  parsedUseKeycloak;

IResourceBuilder<IResource>? keycloak = null;
const int keycloakPort = 8085;
var keycloakHostUrl = $"http://localhost:{keycloakPort}";

if (useKeycloak)
{
    var realmImportPath = Path.GetFullPath(Path.Combine(
        builder.Environment.ContentRootPath,
        "..",
        "..",
        "..",
        "resources",
        "keycloak",
        "realm-export.json"));

    keycloak = builder.AddKeycloak("keycloak", keycloakPort)
        .WithRealmImport(realmImportPath)
        .WithLifetime(ContainerLifetime.Persistent);
}

var crashReportPath = Path.Combine(Path.GetTempPath(), "fig-api-crash.%p");
var figApi = builder.AddProject<Fig_Api>("fig-api")
    .WithHttpsEndpoint(7281, name: "fig-api-https")
    .WithEnvironment("DOTNET_EnableCrashReportOnly", "1")
    .WithEnvironment("DOTNET_DbgMiniDumpName", crashReportPath);

if (useKeycloak)
{
    figApi = figApi
        .WithEnvironment("ApiSettings__Authentication__Mode", "Keycloak")
        .WithEnvironment("ApiSettings__Authentication__Keycloak__Authority", $"{keycloakHostUrl}/realms/fig")
        .WithEnvironment("ApiSettings__Authentication__Keycloak__Audience", "fig-api")
        .WithEnvironment("ApiSettings__Authentication__Keycloak__RequireHttpsMetadata", "false")
        .WaitFor(keycloak!);
}

var figWeb = builder.AddProject<Fig_Web>("fig-web")
    .WithHttpsEndpoint(7148, name: "fig-web-https")
    .WaitFor(figApi);

if (useKeycloak)
{
    // Blazor WASM loads wwwroot/appsettings.{Environment}.json in the browser;
    // process WebSettings__* env vars do not reach that config.
    figWeb = figWeb
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Keycloak")
        .WithEnvironment("DOTNET_ENVIRONMENT", "Keycloak")
        .WaitFor(keycloak!);
}

builder.AddProject<Fig_Examples_AspNetApi>("aspnetapi-example")
    .WithEnvironment("FIG_API_URI", "https://localhost:7281")
    .WithArgs("--secret=f984efe5b49b40ffaf53428cec9530b8");

builder.AddProject<Fig_Examples_Yarp>("yarp")
    .WithEnvironment("FIG_API_URI", "https://localhost:7281")
    .WithArgs("--secret=f984efe5b49b40ffaf53428cec9530b3");

/*
builder.AddProject<Fig_Integration_ConsoleWebHookHandler>("console-webhook")
    .WithEnvironment("FIG_API_URI", "https://localhost:7281")
    .WithArgs("--secret=0352ee79afb2451aaf5733e047bd6c69");

builder.AddProject<Fig_Integration_MicrosoftSentinel>("fig-sentinel-connector")
    .WithEnvironment("FIG_API_URI", "https://localhost:7281")
    .WithArgs("--secret=0352ee79afb2451aaf5733e047bd6c69")
    .WithHttpsEndpoint(7050, name: "sentinel-https");
*/
builder.Build().Run();

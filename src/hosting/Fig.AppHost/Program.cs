using Projects;

var builder = DistributedApplication.CreateBuilder(args);

const int keycloakPort = 8085;
var keycloakHostUrl = $"http://localhost:{keycloakPort}";
var realmImportPath = Path.GetFullPath(Path.Combine(
    builder.Environment.ContentRootPath,
    "..",
    "..",
    "..",
    "resources",
    "keycloak",
    "realm-export.json"));

var keycloak = builder.AddKeycloak("keycloak", keycloakPort)
    .WithRealmImport(realmImportPath)
    .WithLifetime(ContainerLifetime.Persistent);

var figApi = builder.AddProject<Fig_Api>("fig-api")
    .WithHttpsEndpoint(7281, name: "fig-api-https")
    .WithEnvironment("ApiSettings__Authentication__Mode", "Keycloak")
    .WithEnvironment("ApiSettings__Authentication__Keycloak__Authority", $"{keycloakHostUrl}/realms/fig")
    .WithEnvironment("ApiSettings__Authentication__Keycloak__Audience", "fig-api")
    .WithEnvironment("ApiSettings__Authentication__Keycloak__RequireHttpsMetadata", "false")
    .WaitFor(keycloak);

builder.AddProject<Fig_Web>("fig-web")
    .WithHttpsEndpoint(7148, name: "fig-web-https")
    .WithEnvironment("WebSettings__Authentication__Mode", "Keycloak")
    .WithEnvironment("WebSettings__Authentication__Keycloak__Authority", $"{keycloakHostUrl}/realms/fig")
    .WithEnvironment("WebSettings__Authentication__Keycloak__ClientId", "fig-web")
    .WithEnvironment("WebSettings__Authentication__Keycloak__Scopes", "openid profile email roles")
    .WithEnvironment("WebSettings__Authentication__Keycloak__ApiScope", "")
    .WithEnvironment("WebSettings__Authentication__Keycloak__ResponseType", "code")
    .WithEnvironment("WebSettings__Authentication__Keycloak__PostLogoutRedirectUri", "https://localhost:7148/")
    .WithEnvironment("WebSettings__Authentication__Keycloak__AccountManagementUrl", $"{keycloakHostUrl}/realms/fig/account/")
    .WaitFor(keycloak)
    .WaitFor(figApi);

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
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var dbPath = Path.Combine(Path.GetTempPath(), $"fig-e2e-{Guid.NewGuid():N}.db");
const string figApiUri = "https://localhost:7281";

var figApi = builder.AddProject<Fig_Api>("fig-api")
    .WithHttpsEndpoint(7281, name: "fig-api-https")
    .WithEnvironment("ApiSettings__DbConnectionString",
        $"Data Source={dbPath};Version=3;New=True;Busy Timeout=5000")
    .WithEnvironment("ApiSettings__ForceAdminDefaultPasswordChange", "false")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

var figWeb = builder.AddProject<Fig_Web>("fig-web", launchProfileName: "Fig.Web.E2E")
    .WithHttpsEndpoint(7148, name: "fig-web-https")
    .WaitFor(figApi);

builder.AddProject<Fig_Examples_AspNetApi>("aspnetapi-example")
    .WithEnvironment("FIG_API_URI", figApiUri)
    .WithEnvironment("FIG_INSECURE_SSL", "1")
    .WaitFor(figApi);

builder.AddProject<Fig_Examples_DisplayScript>("displayscript-example")
    .WithEnvironment("FIG_API_URI", figApiUri)
    .WithEnvironment("FIG_INSECURE_SSL", "1")
    .WaitFor(figApi);

builder.Build().Run();

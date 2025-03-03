using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL for Keycloak
var postgres = builder.AddPostgres("postgres")
    .WithEnvironment("POSTGRES_USER", "postgres")
    .WithEnvironment("POSTGRES_PASSWORD", "postgres")
    .WithEnvironment("POSTGRES_DB", "keycloak");

// Add Keycloak
builder.AddContainer("keycloak", "quay.io/keycloak/keycloak:latest")
    .WithEnvironment("KEYCLOAK_ADMIN", "admin")
    .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", "admin")
    .WithEnvironment("KC_DB", "postgres")
    .WithEnvironment("KC_DB_URL", "jdbc:postgresql://postgres:5432/keycloak")
    .WithEnvironment("KC_DB_USERNAME", "postgres")
    .WithEnvironment("KC_DB_PASSWORD", "postgres")
    .WithArgs("start-dev")
    .WithHttpEndpoint(port: 8080, name: "keycloak")
    .WithServiceBinding(containerPort: 8080, hostPort: 8443, scheme: "http")
    .WithReference(postgres);

builder.AddProject<Fig_Api>("fig-api")
    .WithHttpsEndpoint(7281);

builder.AddProject<Fig_Web>("fig-web");

builder.AddProject<Fig_Examples_AspNetApi>("aspnetapi-example")
    .WithEnvironment("FIG_API_URI", "https://localhost:7281")
    .WithArgs("--secret=f984efe5b49b40ffaf53428cec9530b8");

builder.AddProject<Fig_Integration_ConsoleWebHookHandler>("console-webhook")
    .WithEnvironment("FIG_API_URI", "https://localhost:7281")
    .WithArgs("--secret=0352ee79afb2451aaf5733e047bd6c69");

builder.AddProject<Fig_Integration_SqlLookupTableService>("sql-lookup-table")
    .WithEnvironment("FIG_API_URI", "https://localhost:7281")
    .WithArgs("--secret=3422fdd2cffd4c9d9e67d1a13e146ca3")
    .WithHttpsEndpoint(7040);

builder.Build().Run();
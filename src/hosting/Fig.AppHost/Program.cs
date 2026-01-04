using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Fig_Api>("fig-api")
    .WithHttpsEndpoint(7281, name: "fig-api-https");

builder.AddProject<Fig_Web>("fig-web")
    .WithHttpsEndpoint(7148, name: "fig-web-https");

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
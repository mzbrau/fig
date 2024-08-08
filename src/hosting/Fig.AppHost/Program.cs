using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Fig_Api>("api").WithHttpsEndpoint(7281);
builder.AddProject<Fig_Web>("web");

builder.Build().Run();
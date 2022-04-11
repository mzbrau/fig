using Fig.Client;
using Fig.Client.Configuration;
using Fig.Client.ExtensionMethods;
using Fig.Examples.AspNetApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var figOptions = new FigOptions();
figOptions.StaticUri("https://localhost:7281");
await builder.Services.AddFig<ISettings, Settings>(figOptions);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
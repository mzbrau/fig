using Fig.Client.ExtensionMethods;
using Fig.Examples.AspNetApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// var loggerFactory = LoggerFactory.Create(b =>
// {
//     b.AddSerilog(logger);
// });

var configuration = new ConfigurationBuilder()
    .AddFig<Settings>(o =>
    {
        o.ClientName = "AspNetApi";
        o.SupportsRestart = true;
    }).Build();

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Logging.AddSerilog(logger);

builder.Services.Configure<Settings>(configuration);

builder.Host.UseFigValidation<Settings>();
builder.Host.UseFigRestart<Settings>();


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
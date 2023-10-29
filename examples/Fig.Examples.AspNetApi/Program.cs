using Fig.Client.ExtensionMethods;
using Fig.Examples.AspNetApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
       
var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Logging.AddSerilog(logger);


builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var loggerFactory = LoggerFactory.Create(b =>
{
    b.AddSerilog(logger);
});

var configuration = new ConfigurationBuilder()
    .AddFig<Settings>(o =>
    {
        o.ClientName = "AspNetApi";
        o.LoggerFactory = loggerFactory;
        o.SupportsRestart = true;
    }).Build();
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
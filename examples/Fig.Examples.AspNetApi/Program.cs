using Fig.Client.ExtensionMethods;
using Fig.Client.Logging;
using Fig.Examples.AspNetApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

await builder.Services.AddFig<ISettings, Settings>(new ConsoleLogger(), options =>
{
    options.ApiUri = new Uri("https://localhost:7281");
    options.ClientSecret = "757bedb7608244c48697710da05db3ca";
});

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
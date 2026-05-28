using Fig.Client.ExtensionMethods;
using Fig.Client.Testing.Extensions;
using Fig.Client.Testing.Integration;
using Fig.Examples.AspNetApi.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Fig.Examples.AspNetApi.Test;

public abstract class IntegrationTestBase
{
    protected readonly Settings Settings = new();
    protected readonly ConfigReloader<Settings> ConfigReloader = new();
    protected HttpClient? Client;
    protected WebApplicationFactory<WeatherForecastController>? Application;

    [OneTimeSetUp]
    public void FixtureSetup()
    {
        Application = new WebApplicationFactory<WeatherForecastController>().WithWebHostBuilder(builder =>
        {
            builder.DisableFig();
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddIntegrationTestConfiguration(ConfigReloader, Settings);
                config.Build();
            });
        });

        Client = Application.CreateClient();
    }

    [OneTimeTearDown]
    public void FixtureTearDown()
    {
        Client?.Dispose();
        Application?.Dispose();
    }
}
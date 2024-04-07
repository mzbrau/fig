using Fig.Client.ExtensionMethods;
using Fig.Client.IntegrationTest;
using Fig.Examples.AspNetApi.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Fig.Examples.AspNetApi.Test;

public class IntegrationTestBase
{
    protected readonly Settings Settings = new();
    protected readonly ConfigReloader ConfigReloader = new();
    protected HttpClient? Client;

    [OneTimeSetUp]
    public void FixtureSetup()
    {
        var application = new WebApplicationFactory<WeatherForecastController>().WithWebHostBuilder(builder =>
        {
            builder.DisableFig();
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddIntegrationTestConfiguration(ConfigReloader, Settings);
                config.Build();
            });
        });

        Client = application.CreateClient();
    }

    [OneTimeTearDown]
    public void FixtureTearDown()
    {
        Client?.Dispose();
    }
}
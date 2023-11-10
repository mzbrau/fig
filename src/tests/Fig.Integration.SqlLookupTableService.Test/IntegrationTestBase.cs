using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Serilog.Events;

namespace Fig.Integration.SqlLookupTableService.Test;

public class IntegrationTestBase
{
    protected readonly Settings settings = new();
    protected readonly HttpClient client;
    protected Mock<IFigFacade> FigFacadeMock = new();
    protected Mock<ISqlQueryManager> SqlQueryManagerMock = new();
    private IConfigurationRoot _configuration;

    public IntegrationTestBase()
    {
        settings.FigUsername = "myUser";
        var application = new WebApplicationFactory<Worker>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("disable-fig", "true");

            builder.ConfigureAppConfiguration((context, conf) =>
            {
                var settings = new Settings();
                settings.FigUsername = "myUser";
                settings.RefreshIntervalSeconds = 5;
                conf.AddObject(settings);

                // here we can "compile" the settings. Api.Setup will do the same, it doesn't matter.
                _configuration = conf.Build();
            });

            builder.ConfigureServices((context, services) =>
            {
                services.AddSingleton(_ => FigFacadeMock.Object);
                services.AddSingleton(_ => SqlQueryManagerMock.Object);
            });
        });

        client = application.CreateClient();
    }

    

    [SetUp]
    public void SetUp()
    {
        settings.FigUsername = "User";
        settings.FigPassword = "Password";
        settings.FigUri = "http://localhost:5050";
        settings.DatabaseConnectionString = "server=x";
        settings.ConnectionStringPassword = "Password";
        settings.Configuration = new List<LookupTableConfiguration>
        {
            new()
            {
                Name = "Test",
                SqlExpression = "SELECT 1"
            }
        };
        settings.LogLevel = LogEventLevel.Debug;
        settings.RefreshIntervalSeconds = 30;

        _configuration.Reload();
    }
}
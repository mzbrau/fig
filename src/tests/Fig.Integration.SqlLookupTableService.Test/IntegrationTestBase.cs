using Fig.Client.ExtensionMethods;
using Fig.Client.IntegrationTest;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Serilog.Events;

namespace Fig.Integration.SqlLookupTableService.Test;

public class IntegrationTestBase
{
    protected readonly Settings Settings = CreateDefault();
    protected readonly Mock<IFigFacade> FigFacadeMock = new();
    protected readonly Mock<ISqlQueryManager> SqlQueryManagerMock = new();
    protected readonly ConfigReloader ConfigReloader = new();

    public IntegrationTestBase()
    {
        var application = new WebApplicationFactory<Worker>().WithWebHostBuilder(builder =>
        {
            builder.DisableFig();
            builder.ConfigureAppConfiguration((a, conf) =>
            {
                conf.AddIntegrationTestConfiguration(ConfigReloader, Settings);
                conf.Build();
            });

            builder.ConfigureServices((_, services) =>
            {
                services.AddSingleton(_ => FigFacadeMock.Object);
                services.AddSingleton(_ => SqlQueryManagerMock.Object);
            });
        });

        application.CreateClient();
    }
    
    private static Settings CreateDefault()
    {
        return new Settings
        {
            FigUsername = "User",
            FigPassword = "Password",
            FigUri = "http://localhost:5050",
            DatabaseConnectionString = "server=x",
            ConnectionStringPassword = "Password",
            Configuration = new List<LookupTableConfiguration>
            {
                new()
                {
                    Name = "Test",
                    SqlExpression = "SELECT 1"
                }
            },
            LogLevel = LogEventLevel.Debug,
            RefreshIntervalSeconds = 5
        };
    }
}
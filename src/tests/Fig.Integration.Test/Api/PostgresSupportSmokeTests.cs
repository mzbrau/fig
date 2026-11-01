using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Fig.Api;
using Fig.Api.Datalayer;
using Fig.Client.Testing.Integration;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NHibernate;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace Fig.Integration.Test.Api;

/// <summary>
/// Smoke coverage for PostgreSQL: SchemaUpdate, data migrations, login, and health.
/// Requires Docker (Testcontainers). Skipped when Docker is unavailable.
/// </summary>
[TestFixture]
[Category("Postgres")]
public class PostgresSupportSmokeTests
{
    private PostgreSqlContainer? _postgres;
    private WebApplicationFactory<ApiSettings>? _app;
    private HttpClient? _client;
    private string _figAppDataDir = string.Empty;
    private string? _previousFigAppDataDir;
    private string? _previousDisableRegistrationChecksum;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        try
        {
            // Override with FIG_TEST_POSTGRES_IMAGE when Docker Hub cannot pull the default tag.
            // Set TESTCONTAINERS_RYUK_DISABLED externally when Ryuk pulls are blocked.
            var postgresImage = Environment.GetEnvironmentVariable("FIG_TEST_POSTGRES_IMAGE")
                ?? "postgres:16";

            _postgres = new PostgreSqlBuilder(postgresImage)
                .WithDatabase("fig")
                .WithUsername("fig_login")
                .WithPassword("fig_password")
                .Build();

            await _postgres.StartAsync();
        }
        catch (Exception ex) when (IsDockerUnavailable(ex))
        {
            Assert.Ignore($"PostgreSQL Testcontainers unavailable (Docker required): {ex.Message}");
            return;
        }

        _previousFigAppDataDir = Environment.GetEnvironmentVariable("FIG_APP_DATA_DIR");
        _previousDisableRegistrationChecksum =
            Environment.GetEnvironmentVariable("FIG_DISABLE_REGISTRATION_CHECKSUM");

        _figAppDataDir = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            $"fig_pg_appdata_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_figAppDataDir);
        Environment.SetEnvironmentVariable("FIG_APP_DATA_DIR", _figAppDataDir);
        Environment.SetEnvironmentVariable("FIG_DISABLE_REGISTRATION_CHECKSUM", "true");

        var settings = new ApiSettings
        {
            DbConnectionString = _postgres.GetConnectionString(),
            DatabaseProvider = DatabaseProvider.PostgreSql,
            Secret = Guid.NewGuid().ToString("N"),
            TokenLifeMinutes = 60,
            SchedulingCheckIntervalMs = 60_000,
            TimeMachineCheckIntervalMs = 0,
            EnableGitHubReleaseDiscovery = false
        };

        var reloadableSource = new ReloadableConfigurationSource<ApiSettings>
        {
            ConfigReloader = new ConfigReloader<ApiSettings>(),
            SettingsType = typeof(ApiSettings),
            InitialConfiguration = settings,
            SectionNameOverride = "ApiSettings"
        };

        _app = new WebApplicationFactory<ApiSettings>().WithWebHostBuilder(builder =>
        {
            IConfigurationRoot? configuration = null;
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.Sources.Clear();
                config.Add(reloadableSource);
                configuration = config.Build();
            });
            builder.ConfigureServices(services =>
            {
                if (configuration is not null)
                    services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"));

                services.PostConfigure<ApiSettings>(opts =>
                {
                    opts.DbConnectionString = settings.DbConnectionString;
                    opts.DatabaseProvider = DatabaseProvider.PostgreSql;
                    opts.Secret = settings.Secret;
                    opts.SchedulingCheckIntervalMs = settings.SchedulingCheckIntervalMs;
                    opts.TimeMachineCheckIntervalMs = settings.TimeMachineCheckIntervalMs;
                    opts.EnableGitHubReleaseDiscovery = settings.EnableGitHubReleaseDiscovery;
                });
            });
        });

        _client = _app.CreateClient();
        _client.Timeout = TimeSpan.FromMinutes(2);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_app is not null)
        {
            var sessionFactory = _app.Services.GetService<ISessionFactory>();
            sessionFactory?.Close();
            sessionFactory?.Dispose();
            await _app.DisposeAsync();
        }

        _client?.Dispose();

        if (_postgres is not null)
            await _postgres.DisposeAsync();

        Environment.SetEnvironmentVariable("FIG_APP_DATA_DIR", _previousFigAppDataDir);
        Environment.SetEnvironmentVariable(
            "FIG_DISABLE_REGISTRATION_CHECKSUM",
            _previousDisableRegistrationChecksum);

        if (!string.IsNullOrEmpty(_figAppDataDir) && Directory.Exists(_figAppDataDir))
        {
            try
            {
                Directory.Delete(_figAppDataDir, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup
            }
        }
    }

    [Test]
    public async Task SchemaMigrationsLoginAndHealth_ShouldSucceed_OnPostgreSql()
    {
        Assert.That(_app, Is.Not.Null);
        Assert.That(_client, Is.Not.Null);
        Assert.That(_postgres, Is.Not.Null);

        using (var scope = _app!.Services.CreateScope())
        {
            var session = scope.ServiceProvider.GetRequiredService<ISession>();
            var migrations = await session
                .CreateQuery("FROM DatabaseMigrationBusinessEntity WHERE Status = :status ORDER BY ExecutionNumber")
                .SetParameter("status", "complete")
                .ListAsync<DatabaseMigrationBusinessEntity>();

            Assert.That(migrations.Count, Is.GreaterThanOrEqualTo(8),
                "Expected all numbered data migrations to complete on PostgreSQL");
            Assert.That(migrations.Select(m => m.ExecutionNumber), Is.EqualTo(Enumerable.Range(1, migrations.Count)));
        }

        var authPayload = JsonConvert.SerializeObject(
            new AuthenticateRequestDataContract("admin", "admin"),
            JsonSettings.FigDefault);
        using var authRequest = new StringContent(authPayload, Encoding.UTF8, "application/json");
        var authResponse = await _client!.PostAsync("/users/authenticate", authRequest);
        Assert.That(authResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var authBody = await authResponse.Content.ReadAsStringAsync();
        var auth = JsonConvert.DeserializeObject<AuthenticateResponseDataContract>(authBody, JsonSettings.FigDefault);
        Assert.That(auth?.Token, Is.Not.Null.And.Not.Empty);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var healthResponse = await _client.GetAsync("/_health");
        Assert.That(healthResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    private static bool IsDockerUnavailable(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            var message = current.Message;
            if (message.Contains("Cannot connect to the Docker daemon", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("docker.sock", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("The system cannot find the file specified", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("No such file or directory", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("Docker is either not running or not accessible", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("Is the docker daemon running", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

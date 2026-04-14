using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Fig.Client.ExtensionMethods;
using Fig.Contracts.Settings;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Fig.Integration.Test.Client;

public class ComplexConfigurationSectionTests : IntegrationTestBase
{
    [Test]
    public void ShallApplyComplexConfigurationSectionsWhenFigIsOfflineAndNoBackup()
    {
        var secret = GetNewSecret();
        using var offlineHttpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:9999"),
        };

        using var context = InitializeAndGetComplexSettings(secret, offlineHttpClient);
        AssertConnections(context, "primary-user", "primary-password", "secondary-user", "secondary-password");
    }

    [Test]
    public void ShallApplyComplexConfigurationSectionsWhenFigIsOnline()
    {
        var secret = GetNewSecret();
        using var context = InitializeAndGetComplexSettings(secret);
        AssertConnections(context, "primary-user", "primary-password", "secondary-user", "secondary-password");
    }

    [Test]
    public async Task ShallApplyComplexConfigurationSectionsWhenValuesHaveBeenUpdated()
    {
        var secret = GetNewSecret();
        using var context = InitializeAndGetComplexSettings(secret);
        var clientName = new ClientWithComplexConfigurationSections().ClientName;

        var settings = new List<SettingDataContract>
        {
            new("Database->Connections", new DataGridSettingDataContract(
            [
                new Dictionary<string, object?>
                {
                    ["UserName"] = "updated-user",
                    ["Password"] = "updated-password",
                },
            ])),
        };

        await SetSettings(clientName, settings);
        context.Configuration.Reload();

        AssertConnections(context, "updated-user", "updated-password");
    }

    private ComplexSettingsContext InitializeAndGetComplexSettings(string clientSecret, HttpClient? httpClientOverride = null)
    {
        var builder = WebApplication.CreateBuilder();
        var settings = new ClientWithComplexConfigurationSections();

        var configuration = new ConfigurationBuilder()
            .AddFig<ClientWithComplexConfigurationSections>(o =>
            {
                o.ClientName = settings.ClientName;
                o.HttpClient = httpClientOverride ?? GetHttpClient();
                o.ClientSecretOverride = clientSecret;
            })
            .Build();

        builder.Services.Configure<ConnectionOverrideSettings>(configuration.GetSection("ConnectionOverrides"));
        builder.Services.Configure<DatabaseSectionSettings>(configuration.GetSection("Database"));

        var app = builder.Build();

        return new ComplexSettingsContext(
            app,
            configuration,
            app.Services.GetRequiredService<IOptionsMonitor<ConnectionOverrideSettings>>(),
            app.Services.GetRequiredService<IOptionsMonitor<DatabaseSectionSettings>>());
    }

    private static void AssertConnections(
        ComplexSettingsContext context,
        string firstUserName,
        string firstPassword,
        string? secondUserName = null,
        string? secondPassword = null)
    {
        Assert.That(context.Configuration["Database:Connections:0:UserName"], Is.EqualTo(firstUserName));
        Assert.That(context.Configuration["ConnectionOverrides:ReplicaConnections:0:Password"], Is.EqualTo(firstPassword));
        Assert.That(context.Configuration["ConnectionOverrides:ReplicaConnections"], Is.Null);

        Assert.That(context.DatabaseSettings.CurrentValue.Connections, Is.Not.Null);
        Assert.That(context.OverrideSettings.CurrentValue.ReplicaConnections, Is.Not.Null);
        Assert.That(context.DatabaseSettings.CurrentValue.Connections!.Count, Is.EqualTo(context.OverrideSettings.CurrentValue.ReplicaConnections!.Count));
        Assert.That(context.DatabaseSettings.CurrentValue.Connections[0].UserName, Is.EqualTo(firstUserName));
        Assert.That(context.DatabaseSettings.CurrentValue.Connections[0].Password, Is.EqualTo(firstPassword));
        Assert.That(context.OverrideSettings.CurrentValue.ReplicaConnections[0].UserName, Is.EqualTo(firstUserName));
        Assert.That(context.OverrideSettings.CurrentValue.ReplicaConnections[0].Password, Is.EqualTo(firstPassword));

        if (secondUserName is null || secondPassword is null)
        {
            Assert.That(context.Configuration["Database:Connections:1:UserName"], Is.Null);
            Assert.That(context.Configuration["ConnectionOverrides:ReplicaConnections:1:UserName"], Is.Null);
            Assert.That(context.DatabaseSettings.CurrentValue.Connections.Count, Is.EqualTo(1));
            Assert.That(context.OverrideSettings.CurrentValue.ReplicaConnections.Count, Is.EqualTo(1));
            return;
        }

        Assert.That(context.Configuration["Database:Connections:1:UserName"], Is.EqualTo(secondUserName));
        Assert.That(context.Configuration["ConnectionOverrides:ReplicaConnections:1:Password"], Is.EqualTo(secondPassword));
        Assert.That(context.DatabaseSettings.CurrentValue.Connections.Count, Is.EqualTo(2));
        Assert.That(context.OverrideSettings.CurrentValue.ReplicaConnections.Count, Is.EqualTo(2));
        Assert.That(context.DatabaseSettings.CurrentValue.Connections[1].UserName, Is.EqualTo(secondUserName));
        Assert.That(context.DatabaseSettings.CurrentValue.Connections[1].Password, Is.EqualTo(secondPassword));
        Assert.That(context.OverrideSettings.CurrentValue.ReplicaConnections[1].UserName, Is.EqualTo(secondUserName));
        Assert.That(context.OverrideSettings.CurrentValue.ReplicaConnections[1].Password, Is.EqualTo(secondPassword));
    }

    private sealed class ComplexSettingsContext : IDisposable
    {
        public ComplexSettingsContext(
            WebApplication application,
            IConfigurationRoot configuration,
            IOptionsMonitor<ConnectionOverrideSettings> overrideSettings,
            IOptionsMonitor<DatabaseSectionSettings> databaseSettings)
        {
            Application = application;
            Configuration = configuration;
            OverrideSettings = overrideSettings;
            DatabaseSettings = databaseSettings;
        }

        public WebApplication Application { get; }

        public IConfigurationRoot Configuration { get; }

        public IOptionsMonitor<ConnectionOverrideSettings> OverrideSettings { get; }

        public IOptionsMonitor<DatabaseSectionSettings> DatabaseSettings { get; }

        public void Dispose()
        {
            Application.StopAsync().GetAwaiter().GetResult();
            Application.DisposeAsync().AsTask().GetAwaiter().GetResult();
            (Configuration as IDisposable)?.Dispose();
        }
    }

    public class ConnectionOverrideSettings
    {
        public List<ComplexConnection>? ReplicaConnections { get; set; }
    }

    public class DatabaseSectionSettings
    {
        public List<ComplexConnection>? Connections { get; set; }
    }
}

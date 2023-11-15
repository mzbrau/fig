using System;
using System.Net.Http;
using Fig.Client.ExtensionMethods;
using Fig.Test.Common.TestSettings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Client;

public class ApiOfflineTests
{
    [Test]
    public void ShallGetDefaultValuesIfApiCannotBeContacted()
    {
        var options = InitializeConfigurationProvider<AllSettingsAndTypes>("mySecret");

        Assert.That(options.CurrentValue.BoolSetting, Is.True);
        Assert.That(options.CurrentValue.DateTimeSetting, Is.Null);
        Assert.That(options.CurrentValue.DoubleSetting, Is.EqualTo(45.3));
        Assert.That(options.CurrentValue.EnumSetting, Is.EqualTo(Pets.Cat));
        Assert.That(options.CurrentValue.IntSetting, Is.EqualTo(34));
        Assert.That(options.CurrentValue.JsonSetting, Is.Null);
        Assert.That(options.CurrentValue.LongSetting, Is.EqualTo(64));
        Assert.That(options.CurrentValue.SecretSetting, Is.EqualTo("SecretString"));
        Assert.That(options.CurrentValue.StringCollectionSetting, Is.Null);
        Assert.That(options.CurrentValue.StringSetting, Is.EqualTo("Cat"));
        Assert.That(options.CurrentValue.TimespanSetting, Is.Null);
        Assert.That(options.CurrentValue.LookupTableSetting, Is.EqualTo(5));
        Assert.That(JsonConvert.SerializeObject(options.CurrentValue.ObjectListSetting), Is.EqualTo(JsonConvert.SerializeObject(AllSettingsAndTypes.GetDefaultObjectList())));
    }
    
    private IOptionsMonitor<T> InitializeConfigurationProvider<T>(string clientSecret) where T : TestSettingsBase
    {
        var builder = WebApplication.CreateBuilder();
        var settings = Activator.CreateInstance<T>();

        var configuration = new ConfigurationBuilder()
            .AddFig<T>(o =>
            {
                o.ClientName = settings.ClientName;
                o.HttpClient = new HttpClient()
                {
                    BaseAddress = new Uri("http://localhost:9999")
                }; // This will not connect to anything
                o.ClientSecretOverride = clientSecret;
            }).Build();

        builder.Services.Configure<T>(configuration);

        var app = builder.Build();

        var options = app.Services.GetRequiredService<IOptionsMonitor<T>>();
        return options;
    }
}
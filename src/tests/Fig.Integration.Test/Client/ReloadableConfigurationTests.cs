using System;
using System.Collections.Generic;
using Fig.Client;
using Fig.Client.ExtensionMethods;
using Fig.Client.IntegrationTest;
using Fig.Test.Common.TestSettings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Client;

[TestFixture]
public class ReloadableConfigurationTests
{
    private readonly ConfigReloader<SettingsBase> _configReloader = new();
    private AllSettingsAndTypes _settings = null!;
    private IOptionsMonitor<AllSettingsAndTypes> _options = null!;

    [SetUp]
    public void Setup()
    {
        _settings = new();
        var builder = WebApplication.CreateBuilder();

        var configuration = new ConfigurationBuilder()
            .AddIntegrationTestConfiguration(_configReloader, _settings)
            .Build();

        builder.Services.Configure<AllSettingsAndTypes>(configuration);
        var app = builder.Build();

        _options = app.Services.GetRequiredService<IOptionsMonitor<AllSettingsAndTypes>>();
    }

    [Test]
    public void ShallLoadInitialSettings()
    {
        AssertSettingsAreSame();
    }

    [Test]
    public void ShallUpdateSettingsWithNewValues()
    {
        _settings.BoolSetting = false;
        _settings.DateTimeSetting = new DateTime(2022, 03, 02, 07, 01, 04);
        _settings.DoubleSetting = 1.1;
        _settings.EnumSetting = Pets.Fish;
        _settings.IntSetting = 2;
        _settings.JsonSetting = new SomeSetting {Key = "New Name", Value = "stuff", MyInt = 99};
        _settings.LongSetting = 3;
        _settings.SecretSetting = "mySecret";
        _settings.StringCollectionSetting = new List<string> {"a", "b", "c"};
        _settings.StringSetting = "New String";
        _settings.TimespanSetting = TimeSpan.FromMinutes(5);
        _settings.LookupTableSetting = 2;
        _settings.ObjectListSetting = new List<SomeSetting>
        {
            new() {Key = "New Name", Value = "stuff", MyInt = 99},
            new() {Key = "New Name 2", Value = "stuff 2", MyInt = 100}
        };
        
        _configReloader.Reload(_settings);
        
        AssertSettingsAreSame();
    }

    [Test]
    public void ShallLoadInitialValueForListCollections()
    {
        Assert.That(JsonConvert.SerializeObject(_options.CurrentValue.ObjectListSetting), Is.EqualTo(JsonConvert.SerializeObject(AllSettingsAndTypes.GetDefaultObjectList())));
    }

    private void AssertSettingsAreSame()
    {
        Assert.That(_settings.ClientName, Is.EqualTo(_options.CurrentValue.ClientName));
        Assert.That(_settings.SecretSetting, Is.EqualTo(_options.CurrentValue.SecretSetting));
        Assert.That(_settings.IntSetting, Is.EqualTo(_options.CurrentValue.IntSetting));
        Assert.That(_settings.BoolSetting, Is.EqualTo(_options.CurrentValue.BoolSetting));
        Assert.That(_settings.DoubleSetting, Is.EqualTo(_options.CurrentValue.DoubleSetting));
        Assert.That(_settings.LongSetting, Is.EqualTo(_options.CurrentValue.LongSetting));
        Assert.That(_settings.DateTimeSetting, Is.EqualTo(_options.CurrentValue.DateTimeSetting));
        Assert.That(_settings.TimespanSetting, Is.EqualTo(_options.CurrentValue.TimespanSetting));
        Assert.That(_settings.EnumSetting, Is.EqualTo(_options.CurrentValue.EnumSetting));
        Assert.That(JsonConvert.SerializeObject(_settings.StringCollectionSetting), Is.EqualTo(JsonConvert.SerializeObject(_options.CurrentValue.StringCollectionSetting)));
        Assert.That(JsonConvert.SerializeObject(_settings.ObjectListSetting), Is.EqualTo(JsonConvert.SerializeObject(_options.CurrentValue.ObjectListSetting)));
        Assert.That(JsonConvert.SerializeObject(_settings.JsonSetting), Is.EquivalentTo(JsonConvert.SerializeObject(_options.CurrentValue.JsonSetting)));
        Assert.That(_settings.LookupTableSetting, Is.EqualTo(_options.CurrentValue.LookupTableSetting));
    }
}
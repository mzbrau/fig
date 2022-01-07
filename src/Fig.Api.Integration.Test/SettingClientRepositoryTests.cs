using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Fig.Api.Datalayer;
using Fig.Api.Datalayer.BusinessEntities;
using Fig.Api.Datalayer.Repositories;
using Fig.Contracts.SettingDefinitions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Api.Integration.Test;

public class Tests
{
    private SettingClientClientRepository? _settingClientRepository;
    
    [SetUp]
    public void Setup()
    {
        _settingClientRepository =
            new SettingClientClientRepository(new FigSessionFactory(Mock.Of<ILogger<FigSessionFactory>>()));
    }

    [Test]
    public void Test1()
    {
        using var app = new WebApplicationFactory<Program>();
        using var client = app.CreateClient();

        var address = client.BaseAddress;
        
        Assert.That(address, Is.Not.Null);
        
        var json = JsonConvert.SerializeObject(new SettingsClientDefinitionDataContract()
        {
            Name = "MyTest",
            Settings = new List<SettingDefinitionDataContract>()
            {
                new()
                {
                    Name = "TestSetting",
                    Description = "Some desc",
                    DefaultValue = "Dog"
                }
            }
        });
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        client.DefaultRequestHeaders.Add("clientSecret", "123");
        var result = client.PostAsync("/api/clients", data).Result;
        
        Assert.That(result.IsSuccessStatusCode, Is.True);
    }
    
    [Test]
    public void ShallSaveAndGet()
    {
        var client = new SettingClientBusinessEntity
        {
            Name = "My Test client",
            ClientSecret = "secret123",
            Settings = new List<SettingBusinessEntity>()
            {
                new()
                {
                    Name = "Some Setting",
                    Description = "This is a setting",
                    Value = "This is a string value",
                    DefaultValue = true
                },
                new()
                {
                    Name = "Another Setting",
                    Description = "This is a second setting",
                    Value = DateTime.Now,
                    DefaultValue = new List<string>() {"One", "two"}
                }
            }
        };

        var id = _settingClientRepository.RegisterClient(client);

        var clientClone = _settingClientRepository.GetClient(id);

        client.Should().BeEquivalentTo(clientClone);
    }

    [Test]
    public void ShallUpdate()
    {
        var client = new SettingClientBusinessEntity
        {
            Name = "My Test client",
            ClientSecret = "secret123"
        };

        var id = _settingClientRepository.RegisterClient(client);

        var clientClone = _settingClientRepository.GetClient(id);

        clientClone.Name = "NewName";
        
        _settingClientRepository.UpdateClient(clientClone);

        var clientClone2 = _settingClientRepository.GetClient(id);
        
        Assert.That(clientClone2.Name, Is.EqualTo("NewName"));
    }
}
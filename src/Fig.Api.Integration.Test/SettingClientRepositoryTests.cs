using System;
using System.Collections.Generic;
using Fig.Api.Datalayer;
using Fig.Api.Datalayer.BusinessEntities;
using Fig.Api.Datalayer.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
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
using System;
using Fig.Api.BusinessEntities;
using Fig.Api.Datalayer;
using Fig.Api.Repositories;
using FluentAssertions;
using NHibernate.Tool.hbm2ddl;
using NUnit.Framework;

namespace Fig.Api.Integration.Test;

public class Tests
{
    private SettingClientRepository _repository;
    
    [SetUp]
    public void Setup()
    {
        _repository = new SettingClientRepository();
    }

    [Test]
    public void ShallLoadSchema()
    {
        var schemaUpdate = new SchemaUpdate(NHibernateHelper.Configuration);
        schemaUpdate.Execute(Console.WriteLine, true);
    }
    
    [Test]
    public void ShallSaveAndGet()
    {
        var client = new SettingsClientBusinessEntity
        {
            Name = "My Test client",
            ClientSecret = "secret123"
        };

        var id = _repository.Save(client);

        var clientClone = _repository.Get(id);

        client.Should().BeEquivalentTo(clientClone);
    }

    [Test]
    public void ShallUpdate()
    {
        var client = new SettingsClientBusinessEntity
        {
            Name = "My Test client",
            ClientSecret = "secret123"
        };

        var id = _repository.Save(client);

        var clientClone = _repository.Get(id);

        clientClone.Name = "NewName";
        
        _repository.Update(clientClone);

        var clientClone2 = _repository.Get(id);
        
        Assert.That(clientClone2.Name, Is.EqualTo("NewName"));
    }
}
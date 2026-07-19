using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Fig.Api.Datalayer.Repositories;
using Fig.Common.Events;
using Fig.Common.NetStandard.Scripting;
using Fig.Contracts.SettingClients;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.SettingGroups;
using Fig.Contracts.Settings;
using Fig.Datalayer.BusinessEntities;
using Fig.Web;
using Fig.Web.Converters;
using Fig.Web.Facades;
using Fig.Web.Models.Setting;
using Fig.Web.Models.Setting.ConfigurationModels;
using Fig.Web.Notifications;
using Fig.Web.Services;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Radzen;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class SettingClientFacadeLoadPerformanceTests
{
    private Mock<IHttpService> _httpService = null!;
    private Mock<ISettingsDefinitionConverter> _definitionConverter = null!;
    private Mock<IApiVersionFacade> _apiVersionFacade = null!;
    private SettingClientFacade _sut = null!;
    private SettingClientConfigurationModel _client = null!;

    [SetUp]
    public void SetUp()
    {
        _httpService = new Mock<IHttpService>();
        _definitionConverter = new Mock<ISettingsDefinitionConverter>();
        _apiVersionFacade = new Mock<IApiVersionFacade>();
        _apiVersionFacade.SetupGet(f => f.AreSettingsStale).Returns(true);

        _sut = new SettingClientFacade(
            _httpService.Object,
            _definitionConverter.Object,
            Mock.Of<ISettingHistoryConverter>(),
            Mock.Of<IScriptRunner>(),
            Options.Create(new WebSettings()),
            new NotificationService(),
            Mock.Of<INotificationFactory>(),
            Mock.Of<IClientStatusFacade>(),
            Mock.Of<IEventDistributor>(),
            _apiVersionFacade.Object,
            Mock.Of<ISchedulingFacade>(),
            Mock.Of<IDisplayScriptStatusService>());

        _client = new SettingClientConfigurationModel(
            "ClientA",
            "Loading...",
            null,
            false,
            Mock.Of<IScriptRunner>());

        _httpService
            .Setup(service => service.GetLarge<List<SettingsClientDefinitionDataContract>>("/clients", It.IsAny<bool>()))
            .ReturnsAsync(new List<SettingsClientDefinitionDataContract>());

        _httpService
            .Setup(service => service.Get<List<SettingGroupDataContract>>("settinggroups", It.IsAny<bool>()))
            .ReturnsAsync(new List<SettingGroupDataContract>());

        _definitionConverter
            .Setup(converter => converter.Convert(
                It.IsAny<IList<SettingsClientDefinitionDataContract>>(),
                It.IsAny<Action<(string, double)>>()))
            .ReturnsAsync(new List<SettingClientConfigurationModel> { _client });
    }

    [Test]
    public async Task LoadAllClients_FetchesClientsAndSettingGroups()
    {
        await _sut.LoadAllClients();

        _httpService.Verify(
            service => service.GetLarge<List<SettingsClientDefinitionDataContract>>("/clients", It.IsAny<bool>()),
            Times.Once);
        _httpService.Verify(
            service => service.Get<List<SettingGroupDataContract>>("settinggroups", It.IsAny<bool>()),
            Times.Once);
        Assert.That(_sut.SettingClients.Any(c => c.Name == "ClientA"), Is.True);
    }

    [Test]
    public async Task LoadAllClients_BuildsGroupsFromFetchedContracts()
    {
        var timeoutSetting = new StringSettingConfigurationModel(
            new SettingDefinitionDataContract(
                "Timeout",
                "Timeout description",
                new StringSettingDataContract("30"),
                false,
                typeof(string)),
            _client,
            new SettingPresentation(false));
        _client.Settings.Add(timeoutSetting);

        _httpService
            .Setup(service => service.Get<List<SettingGroupDataContract>>("settinggroups", It.IsAny<bool>()))
            .ReturnsAsync(new List<SettingGroupDataContract>
            {
                new(Guid.NewGuid(), "SharedGroup", null, new List<GroupedSettingDataContract>
                {
                    new("Timeout", null, "System.String",
                        new List<SourceSettingDataContract> { new("ClientA", "Timeout") })
                })
            });

        await _sut.LoadAllClients();

        Assert.That(_sut.SettingClients.Any(c => c.IsGroup && c.Name == "SharedGroup"), Is.True);
    }

    [Test]
    public async Task LoadClientDescriptions_LoadsOnceAndUpdatesClientDescription()
    {
        await _sut.LoadAllClients();

        _httpService
            .Setup(service => service.GetLarge<ClientsDescriptionDataContract>("/clients/descriptions", It.IsAny<bool>()))
            .ReturnsAsync(new ClientsDescriptionDataContract(
            [
                new ClientDescriptionDataContract("ClientA", "# Client A markdown")
            ]));
        _httpService
            .Setup(service => service.Post("/diagnostics/web-client-load", It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        await _sut.LoadClientDescriptions();
        await _sut.LoadClientDescriptions();

        _httpService.Verify(
            service => service.GetLarge<ClientsDescriptionDataContract>("/clients/descriptions", It.IsAny<bool>()),
            Times.Once);
        Assert.That(_client.Description, Is.EqualTo("# Client A markdown"));
    }

    [Test]
    public void CloneForBestEffortRead_DoesNotCopyDescriptionOrRunSessions()
    {
        var source = new SettingClientBusinessEntity
        {
            Name = "ClientA",
            Description = "Large markdown that should not be hydrated on GetAllClients",
            Settings = new List<SettingBusinessEntity>(),
            CustomActions = new List<CustomActionBusinessEntity>(),
            RunSessions = new List<ClientRunSessionBusinessEntity>
            {
                new() { RunSessionId = Guid.NewGuid() }
            }
        };

        var method = typeof(SettingClientRepository).GetMethod(
            "CloneForBestEffortRead",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);

        var clone = (SettingClientBusinessEntity)method!.Invoke(null, [source])!;

        Assert.That(clone.Name, Is.EqualTo("ClientA"));
        Assert.That(clone.Description, Is.Empty);
        Assert.That(clone.RunSessions, Is.Empty);
        Assert.That(source.Description, Is.EqualTo("Large markdown that should not be hydrated on GetAllClients"));
        Assert.That(source.RunSessions.Count, Is.EqualTo(1));
    }
}

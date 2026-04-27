using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Common.Events;
using Fig.Common.NetStandard.Scripting;
using Fig.Contracts.SettingClients;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.SettingGroups;
using Fig.Contracts.Settings;
using Fig.Web;
using Fig.Web.Converters;
using Fig.Web.Events;
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
public class SettingClientFacadeGroupMetadataTests
{
    private Mock<IHttpService> _httpService = null!;
    private Mock<ISettingsDefinitionConverter> _definitionConverter = null!;
    private SettingClientFacade _sut = null!;
    private SettingClientConfigurationModel _clientA = null!;
    private SettingClientConfigurationModel _clientB = null!;

    [SetUp]
    public void SetUp()
    {
        _httpService = new Mock<IHttpService>();
        _definitionConverter = new Mock<ISettingsDefinitionConverter>();
        var historyConverter = new Mock<ISettingHistoryConverter>();
        var scriptRunner = new Mock<IScriptRunner>();
        var webSettings = Options.Create(new WebSettings());
        var notificationService = new NotificationService();
        var notificationFactory = new Mock<INotificationFactory>();
        var clientStatusFacade = new Mock<IClientStatusFacade>();
        var eventDistributor = new Mock<IEventDistributor>();
        var apiVersionFacade = new Mock<IApiVersionFacade>();
        var schedulingFacade = new Mock<ISchedulingFacade>();

        _sut = new SettingClientFacade(
            _httpService.Object,
            _definitionConverter.Object,
            historyConverter.Object,
            scriptRunner.Object,
            webSettings,
            notificationService,
            notificationFactory.Object,
            clientStatusFacade.Object,
            eventDistributor.Object,
            apiVersionFacade.Object,
            schedulingFacade.Object,
            Mock.Of<IDisplayScriptStatusService>());

        _clientA = CreateClient("ClientA");
        _clientB = CreateClient("ClientB");
        _clientA.Settings.Add(CreateStringSetting(_clientA, "Timeout", "AAA",
            categoryName: "Category A", categoryColor: "#11AA11", validationRegex: "^A+$"));
        _clientB.Settings.Add(CreateStringSetting(_clientB, "Timeout", "BBB",
            categoryName: "Category B", categoryColor: "#2244DD", validationRegex: "^B+$"));

        _httpService
            .Setup(service => service.GetLarge<List<SettingsClientDefinitionDataContract>>("/clients", It.IsAny<bool>()))
            .ReturnsAsync(new List<SettingsClientDefinitionDataContract>());

        _definitionConverter
            .Setup(converter => converter.Convert(
                It.IsAny<IList<SettingsClientDefinitionDataContract>>(),
                It.IsAny<Action<(string, double)>>() ))
            .ReturnsAsync(new List<SettingClientConfigurationModel> { _clientA, _clientB });
    }

    [Test]
    public async Task ShallShowCustomGroupedSettingNameOnSettingsPage()
    {
        SetupGroupsResponse(new List<SettingGroupDataContract>
        {
            new(Guid.NewGuid(), "SharedGroup", null, new List<GroupedSettingDataContract>
            {
                new("Friendly Timeout", null, "System.String",
                    new List<SourceSettingDataContract>
                    {
                        new("ClientA", "Timeout"),
                        new("ClientB", "Timeout")
                    })
            })
        });

        await _sut.LoadAllClients();

        var groupClient = _sut.SettingClients.Single(client => client.IsGroup && client.Name == "SharedGroup");
        var groupSetting = (StringSettingConfigurationModel)groupClient.Settings.Single();

        Assert.That(groupSetting.Name, Is.EqualTo("Timeout"));
        Assert.That(groupSetting.DisplayName, Is.EqualTo("Friendly Timeout"));
    }

    [Test]
    public async Task ShallShowCustomGroupedSettingDescriptionOnSettingsPage()
    {
        SetupGroupsResponse(new List<SettingGroupDataContract>
        {
            new(Guid.NewGuid(), "SharedGroup", null, new List<GroupedSettingDataContract>
            {
                new("Timeout", "Grouped timeout description", "System.String",
                    new List<SourceSettingDataContract>
                    {
                        new("ClientA", "Timeout"),
                        new("ClientB", "Timeout")
                    })
            })
        });

        await _sut.LoadAllClients();

        var groupClient = _sut.SettingClients.Single(client => client.IsGroup && client.Name == "SharedGroup");
        var groupSetting = (StringSettingConfigurationModel)groupClient.Settings.Single();

        Assert.That(groupSetting.RawDescription, Is.EqualTo("Grouped timeout description"));
    }

    [Test]
    public async Task ShallAllowGroupedSettingDescriptionToBeExplicitlyCleared()
    {
        SetupGroupsResponse(new List<SettingGroupDataContract>
        {
            new(Guid.NewGuid(), "SharedGroup", null, new List<GroupedSettingDataContract>
            {
                new("Timeout", string.Empty, "System.String",
                    new List<SourceSettingDataContract>
                    {
                        new("ClientA", "Timeout"),
                        new("ClientB", "Timeout")
                    })
            })
        });

        await _sut.LoadAllClients();

        var groupClient = _sut.SettingClients.Single(client => client.IsGroup && client.Name == "SharedGroup");
        var groupSetting = (StringSettingConfigurationModel)groupClient.Settings.Single();

        Assert.That(groupSetting.RawDescription, Is.Empty);
    }

    [Test]
    public async Task ShallIgnoreStoredCategoryOverridesAndUsePrimarySourceCategory()
    {
        SetupGroupsResponse(new List<SettingGroupDataContract>
        {
            new(Guid.NewGuid(), "SharedGroup", null, new List<GroupedSettingDataContract>
            {
                new("Timeout", null, "System.String",
                    new List<SourceSettingDataContract>
                    {
                        new("ClientA", "Timeout"),
                        new("ClientB", "Timeout")
                    })
                {
                    CategoryName = "Stored Category",
                    CategoryColor = "#FF3366"
                }
            })
        });

        await _sut.LoadAllClients();

        var groupClient = _sut.SettingClients.Single(client => client.IsGroup && client.Name == "SharedGroup");
        var groupSetting = (StringSettingConfigurationModel)groupClient.Settings.Single();

        Assert.That(groupSetting.CategoryName, Is.EqualTo("Category A"));
        Assert.That(groupSetting.CategoryColor, Is.EqualTo("#11AA11"));
    }

    [Test]
    public async Task ShallUseSourceOrderToChoosePrimarySourceMetadata()
    {
        SetupGroupsResponse(new List<SettingGroupDataContract>
        {
            new(Guid.NewGuid(), "SharedGroup", null, new List<GroupedSettingDataContract>
            {
                new("Timeout", null, "System.String",
                    new List<SourceSettingDataContract>
                    {
                        new("ClientB", "Timeout"),
                        new("ClientA", "Timeout")
                    })
            })
        });

        await _sut.LoadAllClients();

        var groupClient = _sut.SettingClients.Single(client => client.IsGroup && client.Name == "SharedGroup");
        var groupSetting = (StringSettingConfigurationModel)groupClient.Settings.Single();

        Assert.That(groupSetting.CategoryName, Is.EqualTo("Category B"));
        Assert.That(groupSetting.CategoryColor, Is.EqualTo("#2244DD"));
        Assert.That(groupSetting.ValidationRegex, Is.EqualTo("^B+$"));
    }

    private void SetupGroupsResponse(List<SettingGroupDataContract> groups)
    {
        _httpService
            .Setup(service => service.Get<List<SettingGroupDataContract>>("settinggroups", It.IsAny<bool>()))
            .ReturnsAsync(groups);
    }

    private static SettingClientConfigurationModel CreateClient(string clientName)
    {
        var client = new SettingClientConfigurationModel(clientName, $"{clientName} description", null, false, Mock.Of<IScriptRunner>());
        return client;
    }

    private static StringSettingConfigurationModel CreateStringSetting(
        SettingClientConfigurationModel parent,
        string name,
        string value,
        string? categoryName = null,
        string? categoryColor = null,
        string? validationRegex = null)
    {
        var definition = new SettingDefinitionDataContract(
            name,
            $"{name} description",
            new StringSettingDataContract(value),
            false,
            typeof(string),
            validationRegex: validationRegex,
            categoryName: categoryName,
            categoryColor: categoryColor);

        return new StringSettingConfigurationModel(
            definition,
            parent,
            new SettingPresentation(false));
    }
}

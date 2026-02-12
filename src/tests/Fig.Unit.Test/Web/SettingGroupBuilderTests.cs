using System.Collections.Generic;
using System.Linq;
using Fig.Common.NetStandard.Scripting;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web.Builders;
using Fig.Web.Models.Setting;
using Fig.Web.Models.Setting.ConfigurationModels;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class SettingGroupBuilderTests
{
    [Test]
    public void ShallGroupByLeafNameAndLinkAllOriginalSettings()
    {
        var scriptRunner = Mock.Of<IScriptRunner>();
        var groupBuilder = new SettingGroupBuilder(scriptRunner);

        var client = new SettingClientConfigurationModel("ClientA", "desc", null, false, scriptRunner);

        var nestedUsername = CreateStringSetting(client, "MessageBus->Auth->Username", "MessageBusAuth", "value-a");
        var flatUsername = CreateStringSetting(client, "MessageBus->Username", "MessageBusAuth", "value-b");
        var password = CreateStringSetting(client, "MessageBus->Auth->Password", "MessageBusAuth", "value-c");
        client.Settings = new List<ISetting> { nestedUsername, flatUsername, password };

        var groups = groupBuilder.BuildGroups(new[] { client }).ToList();

        Assert.That(groups.Count, Is.EqualTo(1));
        var group = groups.Single();

        Assert.That(group.Settings.Count, Is.EqualTo(2), "Username leaf should be de-duplicated into one grouped setting.");

        var groupedUsername = group.Settings.Single(s => s.Name == "MessageBus->Auth->Username");
        Assert.That(groupedUsername.GroupManagedSettings, Is.Not.Null);
        Assert.That(groupedUsername.GroupManagedSettings!.Count, Is.EqualTo(2));
        Assert.That(groupedUsername.GroupManagedSettings, Has.Member(nestedUsername));
        Assert.That(groupedUsername.GroupManagedSettings, Has.Member(flatUsername));

        Assert.That(nestedUsername.IsGroupManaged, Is.True);
        Assert.That(flatUsername.IsGroupManaged, Is.True);
        Assert.That(password.IsGroupManaged, Is.True);
    }

    private static StringSettingConfigurationModel CreateStringSetting(
        SettingClientConfigurationModel parent,
        string name,
        string group,
        string value)
    {
        var definition = new SettingDefinitionDataContract(
            name,
            "desc",
            new StringSettingDataContract(value),
            false,
            typeof(string),
            @group: group);

        return new StringSettingConfigurationModel(definition, parent, new SettingPresentation(false));
    }
}
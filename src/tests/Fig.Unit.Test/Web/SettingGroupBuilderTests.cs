using System.Collections.Generic;
using System.Linq;
using Fig.Common.NetStandard.Scripting;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web;
using Fig.Web.Builders;
using Fig.Web.Models.Setting;
using Fig.Web.Models.Setting.ConfigurationModels;
using Microsoft.Extensions.Options;
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
        var webSettings = Options.Create(new WebSettings());
        var groupBuilder = new SettingGroupBuilder(scriptRunner, webSettings);

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

    [Test]
    public void ShallSetIsCompactViewOnGroupSettingsBasedOnWebSettings()
    {
        var scriptRunner = Mock.Of<IScriptRunner>();
        var webSettings = Options.Create(new WebSettings { DefaultDisplayCollapsed = true });
        var groupBuilder = new SettingGroupBuilder(scriptRunner, webSettings);

        var client = new SettingClientConfigurationModel("ClientA", "desc", null, false, scriptRunner);
        var setting = CreateStringSetting(client, "Timeout", "SharedGroup", "100");
        client.Settings = new List<ISetting> { setting };

        var groups = groupBuilder.BuildGroups(new[] { client }).ToList();
        var groupSetting = groups.Single().Settings.Single();

        Assert.That(groupSetting.IsCompactView, Is.True, "Group settings should be collapsed by default when DefaultDisplayCollapsed is true.");
    }

    [Test]
    public void ShallSetIsCompactViewToFalseWhenDefaultDisplayCollapsedIsFalse()
    {
        var scriptRunner = Mock.Of<IScriptRunner>();
        var webSettings = Options.Create(new WebSettings { DefaultDisplayCollapsed = false });
        var groupBuilder = new SettingGroupBuilder(scriptRunner, webSettings);

        var client = new SettingClientConfigurationModel("ClientA", "desc", null, false, scriptRunner);
        var setting = CreateStringSetting(client, "Timeout", "SharedGroup", "100");
        client.Settings = new List<ISetting> { setting };

        var groups = groupBuilder.BuildGroups(new[] { client }).ToList();
        var groupSetting = groups.Single().Settings.Single();

        Assert.That(groupSetting.IsCompactView, Is.False, "Group settings should be expanded when DefaultDisplayCollapsed is false.");
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
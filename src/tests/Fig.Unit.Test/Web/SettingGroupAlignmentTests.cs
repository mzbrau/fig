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
public class SettingGroupAlignmentTests
{
    private IScriptRunner _scriptRunner = null!;
    private SettingGroupBuilder _groupBuilder = null!;

    [SetUp]
    public void SetUp()
    {
        _scriptRunner = Mock.Of<IScriptRunner>();
        _groupBuilder = new SettingGroupBuilder(_scriptRunner);
    }

    [Test]
    public void ShallDetectMisalignedGroupValues()
    {
        var clientA = new SettingClientConfigurationModel("ClientA", "desc", null, false, _scriptRunner);
        var clientB = new SettingClientConfigurationModel("ClientB", "desc", null, false, _scriptRunner);

        var settingA = CreateStringSetting(clientA, "Timeout", "SharedGroup", "100");
        var settingB = CreateStringSetting(clientB, "Timeout", "SharedGroup", "200");
        clientA.Settings = new List<ISetting> { settingA };
        clientB.Settings = new List<ISetting> { settingB };

        var groups = _groupBuilder.BuildGroups(new[] { clientA, clientB }).ToList();

        Assert.That(groups.Count, Is.EqualTo(1));
        var group = groups.Single();
        var groupSetting = group.Settings.Single();

        Assert.That(groupSetting.HasMisalignedGroupValues, Is.True);
        Assert.That(group.HasMisalignedGroupValues, Is.True);
    }

    [Test]
    public void ShallDetectAlignedGroupValues()
    {
        var clientA = new SettingClientConfigurationModel("ClientA", "desc", null, false, _scriptRunner);
        var clientB = new SettingClientConfigurationModel("ClientB", "desc", null, false, _scriptRunner);

        var settingA = CreateStringSetting(clientA, "Timeout", "SharedGroup", "100");
        var settingB = CreateStringSetting(clientB, "Timeout", "SharedGroup", "100");
        clientA.Settings = new List<ISetting> { settingA };
        clientB.Settings = new List<ISetting> { settingB };

        var groups = _groupBuilder.BuildGroups(new[] { clientA, clientB }).ToList();

        var group = groups.Single();
        var groupSetting = group.Settings.Single();

        Assert.That(groupSetting.HasMisalignedGroupValues, Is.False);
        Assert.That(group.HasMisalignedGroupValues, Is.False);
    }

    [Test]
    public void ShallUpdateAlignmentWhenGroupValueChangesAlignsManagedSettings()
    {
        var clientA = new SettingClientConfigurationModel("ClientA", "desc", null, false, _scriptRunner);
        var clientB = new SettingClientConfigurationModel("ClientB", "desc", null, false, _scriptRunner);

        var settingA = CreateStringSetting(clientA, "Timeout", "SharedGroup", "100");
        var settingB = CreateStringSetting(clientB, "Timeout", "SharedGroup", "200");
        clientA.Settings = new List<ISetting> { settingA };
        clientB.Settings = new List<ISetting> { settingB };

        var groups = _groupBuilder.BuildGroups(new[] { clientA, clientB }).ToList();
        var groupSetting = groups.Single().Settings.Single();

        Assert.That(groupSetting.HasMisalignedGroupValues, Is.True);

        // Setting the group value should propagate to all managed settings
        groupSetting.SetValue("300");

        Assert.That(groupSetting.HasMisalignedGroupValues, Is.False);
    }

    [Test]
    public void ShallReportNoMisalignmentWhenNoGroupManagedSettings()
    {
        var client = new SettingClientConfigurationModel("ClientA", "desc", null, false, _scriptRunner);

        var setting = CreateStringSetting(client, "Timeout", null, "100");
        client.Settings = new List<ISetting> { setting };

        Assert.That(setting.HasMisalignedGroupValues, Is.False);
    }

    [Test]
    public void ShallDetectMisalignmentAcrossMultipleSettings()
    {
        var clientA = new SettingClientConfigurationModel("ClientA", "desc", null, false, _scriptRunner);
        var clientB = new SettingClientConfigurationModel("ClientB", "desc", null, false, _scriptRunner);

        var timeoutA = CreateStringSetting(clientA, "Timeout", "SharedGroup", "100");
        var timeoutB = CreateStringSetting(clientB, "Timeout", "SharedGroup", "100");
        var urlA = CreateStringSetting(clientA, "Url", "SharedGroup", "http://a");
        var urlB = CreateStringSetting(clientB, "Url", "SharedGroup", "http://b");
        clientA.Settings = new List<ISetting> { timeoutA, urlA };
        clientB.Settings = new List<ISetting> { timeoutB, urlB };

        var groups = _groupBuilder.BuildGroups(new[] { clientA, clientB }).ToList();
        var group = groups.Single();

        var timeoutGroup = group.Settings.First(s => s.Name == "Timeout");
        var urlGroup = group.Settings.First(s => s.Name == "Url");

        Assert.That(timeoutGroup.HasMisalignedGroupValues, Is.False);
        Assert.That(urlGroup.HasMisalignedGroupValues, Is.True);
        Assert.That(group.HasMisalignedGroupValues, Is.True);
    }

    private static StringSettingConfigurationModel CreateStringSetting(
        SettingClientConfigurationModel parent,
        string name,
        string? group,
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

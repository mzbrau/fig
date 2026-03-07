using Fig.Common.NetStandard.Scripting;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web.Models.Setting;
using Fig.Web.Models.Setting.ConfigurationModels;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class SettingVisibilityTests
{
    private SettingClientConfigurationModel _parent = null!;
    private SettingPresentation _presentation = null!;

    [SetUp]
    public void SetUp()
    {
        _parent = new SettingClientConfigurationModel("TestClient", "Test Client", null, false, Mock.Of<IScriptRunner>());
        _presentation = new SettingPresentation(false);
    }

    [Test]
    public void AdvancedSetting_IsHiddenByDefault()
    {
        // Arrange
        var setting = CreateBoolSetting("AdvSetting", advanced: true);
        _parent.Settings = new System.Collections.Generic.List<ISetting> { setting };

        // Assert - advanced settings start hidden (showAdvanced defaults to false)
        Assert.That(setting.Hidden, Is.True);
    }

    [Test]
    public void AdvancedSetting_BecomesVisibleWhenShowAdvancedEnabled()
    {
        // Arrange
        var setting = CreateBoolSetting("AdvSetting", advanced: true);
        _parent.Settings = new System.Collections.Generic.List<ISetting> { setting };

        // Act
        _parent.ShowAdvancedChanged(true);

        // Assert
        Assert.That(setting.Hidden, Is.False);
    }

    [Test]
    public void AdvancedSetting_BecomesHiddenWhenShowAdvancedDisabled()
    {
        // Arrange
        var setting = CreateBoolSetting("AdvSetting", advanced: true);
        _parent.Settings = new System.Collections.Generic.List<ISetting> { setting };
        _parent.ShowAdvancedChanged(true);

        // Act
        _parent.ShowAdvancedChanged(false);

        // Assert
        Assert.That(setting.Hidden, Is.True);
    }

    [Test]
    public void NonAdvancedSetting_IsVisibleAfterShowAdvancedInitialized()
    {
        // Arrange
        var setting = CreateBoolSetting("NormalSetting", advanced: false);
        _parent.Settings = new System.Collections.Generic.List<ISetting> { setting };

        // Act - this mirrors what OnInitializedAsync does after loading settings
        _parent.ShowAdvancedChanged(false);

        // Assert
        Assert.That(setting.Hidden, Is.False);
    }

    [Test]
    public void NewSettingsAddedToClient_DoNotInheritShowAdvancedState()
    {
        // Arrange - simulates what happens during a settings reload:
        // existing settings had ShowAdvancedChanged(true), but new settings
        // created from fresh SettingDefinitionDataContracts default to _showAdvanced=false
        var existingSetting = CreateBoolSetting("ExistingSetting", advanced: true);
        _parent.Settings = new System.Collections.Generic.List<ISetting> { existingSetting };
        _parent.ShowAdvancedChanged(true);
        Assert.That(existingSetting.Hidden, Is.False, "Existing setting should be visible");

        // Act - simulate reload: create new setting (as LoadAllClientsInternal would)
        var reloadedParent = new SettingClientConfigurationModel("TestClient", "Test Client", null, false, Mock.Of<IScriptRunner>());
        var newSetting = CreateBoolSetting("AdvSetting", advanced: true, parent: reloadedParent);
        reloadedParent.Settings = new System.Collections.Generic.List<ISetting> { newSetting };

        // Assert - new settings default to _showAdvanced=false, so advanced settings are hidden
        Assert.That(newSetting.Hidden, Is.True, "Newly created advanced setting should be hidden by default");
    }

    [Test]
    public void ReapplyingShowAdvanced_FixesNewSettingsVisibility()
    {
        // Arrange - simulate a settings reload producing new setting instances
        var reloadedParent = new SettingClientConfigurationModel("TestClient", "Test Client", null, false, Mock.Of<IScriptRunner>());
        var newSetting = CreateBoolSetting("AdvSetting", advanced: true, parent: reloadedParent);
        reloadedParent.Settings = new System.Collections.Generic.List<ISetting> { newSetting };
        Assert.That(newSetting.Hidden, Is.True, "Newly created advanced setting should be hidden by default");

        // Act - re-apply ShowAdvancedChanged as the fix does (e.g., from SettingsLoaded event)
        reloadedParent.ShowAdvancedChanged(true);

        // Assert
        Assert.That(newSetting.Hidden, Is.False, "After re-applying ShowAdvancedChanged(true), setting should be visible");
    }

    [Test]
    public void ShowAdvancedChanged_IsIdempotent()
    {
        // Arrange
        var setting = CreateBoolSetting("AdvSetting", advanced: true);
        _parent.Settings = new System.Collections.Generic.List<ISetting> { setting };
        _parent.ShowAdvancedChanged(true);
        Assert.That(setting.Hidden, Is.False);

        // Act - calling ShowAdvancedChanged(true) multiple times should have no side effects
        _parent.ShowAdvancedChanged(true);
        _parent.ShowAdvancedChanged(true);

        // Assert
        Assert.That(setting.Hidden, Is.False);
    }

    [Test]
    public void SetVisibilityFromScript_HidesSetting_IndependentOfShowAdvanced()
    {
        // Arrange
        var setting = CreateBoolSetting("AdvSetting", advanced: true);
        _parent.Settings = new System.Collections.Generic.List<ISetting> { setting };
        _parent.ShowAdvancedChanged(true);
        Assert.That(setting.Hidden, Is.False);

        // Act - script hides the setting
        setting.SetVisibilityFromScript(false);

        // Assert - setting is hidden even though _showAdvanced is true
        Assert.That(setting.Hidden, Is.True);
    }

    [Test]
    public void SetVisibilityFromScript_ShowsSetting_WhenAdvancedIsEnabled()
    {
        // Arrange
        var setting = CreateBoolSetting("AdvSetting", advanced: true);
        _parent.Settings = new System.Collections.Generic.List<ISetting> { setting };
        _parent.ShowAdvancedChanged(true);

        // Act - script says setting is visible
        setting.SetVisibilityFromScript(true);

        // Assert - setting is visible
        Assert.That(setting.Hidden, Is.False);
    }

    private BoolSettingConfigurationModel CreateBoolSetting(string name, bool advanced, SettingClientConfigurationModel? parent = null)
    {
        var dataContract = new SettingDefinitionDataContract(
            name,
            $"Description for {name}",
            new BoolSettingDataContract(false),
            advanced: advanced);

        return new BoolSettingConfigurationModel(dataContract, parent ?? _parent, _presentation);
    }
}

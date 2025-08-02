using System.Collections.Generic;
using Fig.Common.NetStandard.Scripting;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web.Models.Setting;
using Fig.Web.Models.Setting.ConfigurationModels;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class DropDownSettingConfigurationModelTests
{
    private SettingClientConfigurationModel _parent = null!;
    private SettingPresentation _presentation = null!;

    [SetUp]
    public void SetUp()
    {
        _parent = new SettingClientConfigurationModel("TestClient", "Test Client", null, true, Mock.Of<IScriptRunner>());
        _presentation = new SettingPresentation(false);
    }

    [Test]
    public void GetFilteredValidValues_WithValidLookupValue_ShouldReturnFilteredOptions()
    {
        // Arrange
        var originalValidValues = new List<string> { "[Bug]Severity", "[Bug]Priority", "[Incident]Urgency" };
        var dataContract = CreateDataContract(originalValidValues, "IssueType");
        
        var lookupSetting = CreateMockSetting("IssueType", "Bug");
        _parent.Settings = [lookupSetting];
        
        // Act
        var dropDownSetting = new DropDownSettingConfigurationModel(dataContract, _parent, _presentation);
        
        // Assert
        Assert.That(dropDownSetting.ValidValues.Count, Is.EqualTo(2));
        Assert.That(dropDownSetting.ValidValues, Contains.Item("Severity"));
        Assert.That(dropDownSetting.ValidValues, Contains.Item("Priority"));
        Assert.That(dropDownSetting.ValidValues, Does.Not.Contain("Urgency"));
    }

    [Test]
    public void GetFilteredValidValues_WithInvalidLookupValue_ShouldReturnNoOptions()
    {
        // Arrange
        var originalValidValues = new List<string> { "[Bug]Severity", "[Bug]Priority", "[Incident]Urgency" };
        var dataContract = CreateDataContract(originalValidValues, "IssueType");
        
        var lookupSetting = CreateMockSetting("IssueType", "InvalidType");
        _parent.Settings = [lookupSetting];
        
        // Act
        var dropDownSetting = new DropDownSettingConfigurationModel(dataContract, _parent, _presentation);
        
        // Assert
        Assert.That(dropDownSetting.ValidValues.Count, Is.EqualTo(0));
    }

    [Test]
    public void GetFilteredValidValues_WithNoLookupKeySettingName_ShouldReturnAllOptions()
    {
        // Arrange
        var originalValidValues = new List<string> { "Option1", "Option2", "Option3" };
        var dataContract = CreateDataContract(originalValidValues, null);
        
        // Act
        var dropDownSetting = new DropDownSettingConfigurationModel(dataContract, _parent, _presentation);
        
        // Assert
        Assert.That(dropDownSetting.ValidValues.Count, Is.EqualTo(3));
        Assert.That(dropDownSetting.ValidValues, Is.EquivalentTo(originalValidValues));
    }

    [Test]
    public void GetFilteredValidValues_WithNullLookupValue_ShouldReturnAllOptions()
    {
        // Arrange
        var originalValidValues = new List<string> { "[Bug]Severity", "[Bug]Priority", "[Incident]Urgency" };
        var dataContract = CreateDataContract(originalValidValues, "IssueType");
        
        var lookupSetting = CreateMockSetting("IssueType", null);
        _parent.Settings = [lookupSetting];
        
        // Act
        var dropDownSetting = new DropDownSettingConfigurationModel(dataContract, _parent, _presentation);
        
        // Assert
        Assert.That(dropDownSetting.ValidValues.Count, Is.EqualTo(3));
        Assert.That(dropDownSetting.ValidValues, Is.EquivalentTo(originalValidValues));
    }

    [Test]
    public void GetFilteredValidValues_WithEmptyLookupValue_ShouldReturnAllOptions()
    {
        // Arrange
        var originalValidValues = new List<string> { "[Bug]Severity", "[Bug]Priority", "[Incident]Urgency" };
        var dataContract = CreateDataContract(originalValidValues, "IssueType");
        
        var lookupSetting = CreateMockSetting("IssueType", "");
        _parent.Settings = [lookupSetting];
        
        // Act
        var dropDownSetting = new DropDownSettingConfigurationModel(dataContract, _parent, _presentation);
        
        // Assert
        Assert.That(dropDownSetting.ValidValues.Count, Is.EqualTo(3));
        Assert.That(dropDownSetting.ValidValues, Is.EquivalentTo(originalValidValues));
    }

    [Test]
    public void DisplayValue_WithValidLookupValue_ShouldReturnValueWithoutPrefix()
    {
        // Arrange
        var originalValidValues = new List<string> { "[Bug]Severity", "[Bug]Priority" };
        var dataContract = CreateDataContract(originalValidValues, "IssueType");
        
        var lookupSetting = CreateMockSetting("IssueType", "Bug");
        _parent.Settings = [lookupSetting];
        
        var dropDownSetting = new DropDownSettingConfigurationModel(dataContract, _parent, _presentation)
            {
                Value = "Severity"
            };

        // Act & Assert
        Assert.That(dropDownSetting.DisplayValue, Is.EqualTo("Severity"));
    }

    [Test]
    public void DisplayValue_SetValue_ShouldStoreDisplayValueOnly()
    {
        // Arrange
        var originalValidValues = new List<string> { "[Bug]Severity", "[Bug]Priority" };
        var dataContract = CreateDataContract(originalValidValues, "IssueType");
        
        var lookupSetting = CreateMockSetting("IssueType", "Bug");
        _parent.Settings = [lookupSetting];
        
        var dropDownSetting = new DropDownSettingConfigurationModel(dataContract, _parent, _presentation)
            {
                // Act
                DisplayValue = "Priority"
            };

        // Assert
        Assert.That(dropDownSetting.Value, Is.EqualTo("Priority"));
    }

    private SettingDefinitionDataContract CreateDataContract(List<string> validValues, string? lookupKeySettingName)
    {
        return new SettingDefinitionDataContract(
            "TestDropDown",
            "A test dropdown",
            new StringSettingDataContract("defaultValue"),
            false,
            validValues: validValues,
            lookupKeySettingName: lookupKeySettingName);
    }

    private ISetting CreateMockSetting(string name, object? value)
    {
        var mock = new Mock<ISetting>();
        mock.Setup(s => s.Name).Returns(name);
        mock.Setup(s => s.GetValue(It.IsAny<bool>())).Returns(value);
        mock.Setup(s => s.SubscribeToValueChanges(It.IsAny<System.Action<ActionType>>()));
        return mock.Object;
    }
}

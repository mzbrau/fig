using System;
using System.Collections.Generic;
using Fig.Client;
using Fig.Client.Abstractions.Attributes;
using Fig.Client.Abstractions.Enums;
using Fig.Client.DefaultValue;
using Fig.Client.Description;
using Fig.Client.Exceptions;
using Fig.Client.Validation;
using Fig.Contracts.SettingDefinitions;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class SettingFieldLengthValidationTests
{
    private const string Ten = "xxxxxxxxxx";
    private const string Fifty = Ten + Ten + Ten + Ten + Ten;
    private const string TwoFifty = Fifty + Fifty + Fifty + Fifty + Fifty;
    private const string Chars255 = TwoFifty + "xxxxx";
    private const string Chars256 = Chars255 + "x";
    private const string Chars245 = Fifty + Fifty + Fifty + Fifty + Ten + Ten + Ten + Ten + "xxxxx";
    private const string Spaces50 = "                                                  ";
    private const string Spaces200 = Spaces50 + Spaces50 + Spaces50 + Spaces50;
    private const string Spaces43 = "                                           ";
    private const string TooLongColor = "rgba(" + Spaces200 + Spaces43 + "1,2,3,1)";

    private readonly List<string> _envVarsToClean = [];

    [TearDown]
    public void TearDown()
    {
        foreach (var envVar in _envVarsToClean)
        {
            Environment.SetEnvironmentVariable(envVar, null);
        }

        _envVarsToClean.Clear();
    }

    private void SetEnvironmentVariable(string key, string value)
    {
        Environment.SetEnvironmentVariable(key, value);
        _envVarsToClean.Add(key);
    }

    #region Validator unit tests

    [Test]
    public void ValidateMaxLength_WithNullOrEmpty_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
            SettingDefinitionLengthValidator.ValidateMaxLength(null, SettingDefinitionFieldLimits.StandardString, "Field"));
        Assert.DoesNotThrow(() =>
            SettingDefinitionLengthValidator.ValidateMaxLength(string.Empty, SettingDefinitionFieldLimits.StandardString, "Field"));
    }

    [Test]
    public void ValidateMaxLength_AtLimit_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
            SettingDefinitionLengthValidator.ValidateMaxLength(
                Chars255,
                SettingDefinitionFieldLimits.StandardString,
                "Field"));
    }

    [Test]
    public void ValidateMaxLength_OverLimit_ThrowsWithFieldLabelAndLengths()
    {
        var ex = Assert.Throws<InvalidSettingException>(() =>
            SettingDefinitionLengthValidator.ValidateMaxLength(
                Chars256,
                SettingDefinitionFieldLimits.StandardString,
                "[Group] on 'MySetting': GroupName"));

        Assert.That(ex!.Message, Does.Contain("[Group]"));
        Assert.That(ex.Message, Does.Contain("MySetting"));
        Assert.That(ex.Message, Does.Contain("255"));
        Assert.That(ex.Message, Does.Contain("256"));
    }

    [Test]
    public void Validate_WithNullOptionalFields_DoesNotThrow()
    {
        var setting = new SettingDefinitionDataContract("MySetting", "Description");

        Assert.DoesNotThrow(() => SettingDefinitionLengthValidator.Validate(setting));
    }

    [Test]
    public void Validate_WithLongDataGridColumnName_Throws()
    {
        var setting = new SettingDefinitionDataContract(
            "GridSetting",
            "Description",
            dataGridDefinition: new DataGridDefinitionDataContract(
                [new DataGridColumnDataContract(Chars256, typeof(string))],
                false));

        var ex = Assert.Throws<InvalidSettingException>(() =>
            SettingDefinitionLengthValidator.Validate(setting));

        Assert.That(ex!.Message, Does.Contain("DataGrid column name"));
        Assert.That(ex.Message, Does.Contain("255"));
        Assert.That(ex.Message, Does.Contain("GridSetting"));
    }

    [Test]
    public void ValidateClientMetadata_WithNullInstance_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
            SettingDefinitionLengthValidator.ValidateClientMetadata("TestClient", null));
    }

    [Test]
    public void ValidateClientMetadata_WithLongClientName_Throws()
    {
        var ex = Assert.Throws<InvalidSettingException>(() =>
            SettingDefinitionLengthValidator.ValidateClientMetadata(Chars256, null));

        Assert.That(ex!.Message, Does.Contain("Client name"));
        Assert.That(ex.Message, Does.Contain("255"));
    }

    [Test]
    public void ValidateClientMetadata_WithLongInstance_Throws()
    {
        var ex = Assert.Throws<InvalidSettingException>(() =>
            SettingDefinitionLengthValidator.ValidateClientMetadata("TestClient", Chars256));

        Assert.That(ex!.Message, Does.Contain("Client instance"));
        Assert.That(ex.Message, Does.Contain("255"));
    }

    #endregion

    #region CreateDataContract attribute tests

    [Test]
    public void CreateDataContract_WithGroupNameAtLimit_DoesNotThrow()
    {
        var settings = new SettingsWithGroupAtLimit();

        Assert.DoesNotThrow(() => settings.CreateDataContract("TestClient"));
    }

    [Test]
    public void CreateDataContract_WithGroupNameOverLimit_Throws()
    {
        var settings = new SettingsWithLongGroup();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            settings.CreateDataContract("TestClient"));

        Assert.That(ex!.Message, Does.Contain("[Group]"));
        Assert.That(ex.Message, Does.Contain("GroupedSetting"));
        Assert.That(ex.Message, Does.Contain("255"));
    }

    [Test]
    public void CreateDataContract_WithLookupTableKeyOverLimit_Throws()
    {
        var settings = new SettingsWithLongLookupKey();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            settings.CreateDataContract("TestClient"));

        Assert.That(ex!.Message, Does.Contain("[LookupTable]"));
        Assert.That(ex.Message, Does.Contain("LookupTableKey"));
        Assert.That(ex.Message, Does.Contain("255"));
    }

    [Test]
    public void CreateDataContract_WithProviderDefinedLookupKeyOverLimitAfterPrefix_Throws()
    {
        var settings = new SettingsWithLongProviderDefinedLookupKey();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            settings.CreateDataContract("TestClient"));

        Assert.That(ex!.Message, Does.Contain("[LookupTable]"));
        Assert.That(ex.Message, Does.Contain("LookupTableKey"));
        Assert.That(ex.Message, Does.Contain("255"));
    }

    [Test]
    public void CreateDataContract_WithLookupKeySettingNameOverLimit_Throws()
    {
        var settings = new SettingsWithLongLookupKeySettingName();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            settings.CreateDataContract("TestClient"));

        Assert.That(ex!.Message, Does.Contain("[LookupTable]"));
        Assert.That(ex.Message, Does.Contain("KeySettingName"));
        Assert.That(ex.Message, Does.Contain("255"));
    }

    [Test]
    public void CreateDataContract_WithCategoryNameOverLimit_Throws()
    {
        var settings = new SettingsWithLongCategoryName();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            settings.CreateDataContract("TestClient"));

        Assert.That(ex!.Message, Does.Contain("[Category]"));
        Assert.That(ex.Message, Does.Contain("Name"));
        Assert.That(ex.Message, Does.Contain("255"));
    }

    [Test]
    public void CreateDataContract_WithCategoryColorOverLimit_Throws()
    {
        Assert.That(TooLongColor.Length, Is.EqualTo(256));

        var settings = new SettingsWithLongCategoryColor();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            settings.CreateDataContract("TestClient"));

        Assert.That(ex!.Message, Does.Contain("[Category]"));
        Assert.That(ex.Message, Does.Contain("ColorHex"));
        Assert.That(ex.Message, Does.Contain("255"));
    }

    [Test]
    public void CreateDataContract_WithDependsOnPropertyOverLimit_Throws()
    {
        var settings = new SettingsWithLongDependsOn();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            settings.CreateDataContract("TestClient"));

        Assert.That(ex!.Message, Does.Contain("[DependsOn]"));
        Assert.That(ex.Message, Does.Contain("DependsOnProperty"));
        Assert.That(ex.Message, Does.Contain("255"));
    }

    [Test]
    public void CreateDataContract_WithMigrateFromOverLimit_Throws()
    {
        var settings = new SettingsWithLongMigrateFrom();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            settings.CreateDataContract("TestClient"));

        Assert.That(ex!.Message, Does.Contain("[MigrateFrom]"));
        Assert.That(ex.Message, Does.Contain("PreviousSettingName"));
        Assert.That(ex.Message, Does.Contain("255"));
    }

    [Test]
    public void CreateDataContract_WithMigrateFromMigrationMethodOverLimit_Throws()
    {
        var settings = new SettingsWithLongMigrateFromMethod();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            settings.CreateDataContract("TestClient"));

        Assert.That(ex!.Message, Does.Contain("[MigrateFrom]"));
        Assert.That(ex.Message, Does.Contain("MigrationMethodName"));
        Assert.That(ex.Message, Does.Contain("255"));
    }

    [Test]
    public void CreateDataContract_WithClientNameOverLimit_Throws()
    {
        var settings = new SettingsWithValidGroup();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            settings.CreateDataContract(Chars256));

        Assert.That(ex!.Message, Does.Contain("Client name"));
        Assert.That(ex.Message, Does.Contain("255"));
    }

    [Test]
    public void CreateDataContract_WithInstanceOverLimit_Throws()
    {
        var settings = new SettingsWithValidGroup();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            settings.CreateDataContract("TestClient", instance: Chars256));

        Assert.That(ex!.Message, Does.Contain("Client instance"));
        Assert.That(ex.Message, Does.Contain("255"));
    }

    [Test]
    public void CreateDataContract_WithGroupEnvOverrideOverLimit_Throws()
    {
        SetEnvironmentVariable("FIG_GROUPEDSETTING_GROUP", Chars256);
        var settings = new SettingsWithValidGroup();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            settings.CreateDataContract("TestClient"));

        Assert.That(ex!.Message, Does.Contain("[Group]"));
        Assert.That(ex.Message, Does.Contain("GroupedSetting"));
        Assert.That(ex.Message, Does.Contain("255"));
    }

    [Test]
    public void CreateDataContract_WithLookupTableKeyEnvOverrideOverLimit_Throws()
    {
        SetEnvironmentVariable("FIG_LOOKUPSETTING_LOOKUPTABLEKEY", Chars256);
        var settings = new SettingsWithValidLookup();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            settings.CreateDataContract("TestClient"));

        Assert.That(ex!.Message, Does.Contain("[LookupTable]"));
        Assert.That(ex.Message, Does.Contain("LookupTableKey"));
        Assert.That(ex.Message, Does.Contain("255"));
    }

    #endregion

    #region Factory computed name tests

    [Test]
    public void Create_WithSettingNameOverLimit_Throws()
    {
        var factory = CreateFactory();
        var settings = new SettingsWithValidGroup();
        var property = typeof(SettingsWithValidGroup).GetProperty(nameof(SettingsWithValidGroup.GroupedSetting))!;
        var longName = "Nested->" + Chars256;
        var settingDetails = new SettingDetails("", property, "test", longName, settings);

        var ex = Assert.Throws<InvalidSettingException>(() =>
            factory.Create(settingDetails, "TestClient", 0, []));

        Assert.That(ex!.Message, Does.Contain("Setting name"));
        Assert.That(ex.Message, Does.Contain("255"));
    }

    [Test]
    public void Create_WithNestedSettingNameAtLimit_DoesNotThrow()
    {
        var factory = CreateFactory();
        var settings = new SettingsWithValidGroup();
        var property = typeof(SettingsWithValidGroup).GetProperty(nameof(SettingsWithValidGroup.GroupedSetting))!;
        var nameAtLimit = new string('x', SettingDefinitionFieldLimits.StandardString);
        var settingDetails = new SettingDetails("", property, "test", nameAtLimit, settings);

        Assert.DoesNotThrow(() => factory.Create(settingDetails, "TestClient", 0, []));
    }

    [Test]
    public void CreateDataContract_WithLongDataGridColumnName_Throws()
    {
        var settings = new SettingsWithLongDataGridColumn();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            settings.CreateDataContract("TestClient"));

        Assert.That(ex!.Message, Does.Contain("DataGrid column name"));
        Assert.That(ex.Message, Does.Contain("255"));
    }

    #endregion

    private static SettingDefinitionFactory CreateFactory()
    {
        var descriptionProvider = new Mock<IDescriptionProvider>();
        descriptionProvider.Setup(a => a.GetDescription(It.IsAny<string>())).Returns("Desc");
        return new SettingDefinitionFactory(descriptionProvider.Object, new Mock<IDataGridDefaultValueProvider>().Object);
    }

    #region Test settings

    private class SettingsWithGroupAtLimit : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("Grouped setting")]
        [Group(Chars255)]
        public string GroupedSetting { get; set; } = "test";

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    private class SettingsWithLongGroup : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("Grouped setting")]
        [Group(Chars256)]
        public string GroupedSetting { get; set; } = "test";

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    private class SettingsWithValidGroup : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("Grouped setting")]
        [Group("MyGroup")]
        public string GroupedSetting { get; set; } = "test";

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    private class SettingsWithLongLookupKey : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("Lookup setting")]
        [LookupTable(Chars256, LookupSource.UserDefined)]
        public string LookupSetting { get; set; } = "test";

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    private class SettingsWithLongProviderDefinedLookupKey : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("Lookup setting")]
        [LookupTable(Chars245, LookupSource.ProviderDefined)]
        public string LookupSetting { get; set; } = "test";

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    private class SettingsWithLongLookupKeySettingName : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("Lookup setting")]
        [LookupTable("MyTable", LookupSource.UserDefined, Chars256)]
        public string LookupSetting { get; set; } = "test";

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    private class SettingsWithValidLookup : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("Lookup setting")]
        [LookupTable("MyTable", LookupSource.UserDefined)]
        public string LookupSetting { get; set; } = "test";

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    private class SettingsWithLongCategoryName : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("Categorized setting")]
        [Fig.Client.Abstractions.Attributes.Category(Chars256, "#FF0000")]
        public string CategorizedSetting { get; set; } = "test";

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    private class SettingsWithLongCategoryColor : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("Categorized setting")]
        [Fig.Client.Abstractions.Attributes.Category("My Category", TooLongColor)]
        public string CategorizedSetting { get; set; } = "test";

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    private class SettingsWithLongDependsOn : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("Parent setting")]
        public bool ParentSetting { get; set; } = true;

        [Setting("Dependent setting")]
        [DependsOn(Chars256, true)]
        public string DependentSetting { get; set; } = "test";

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    private class SettingsWithLongMigrateFrom : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("Renamed setting")]
        [MigrateFrom(Chars256)]
        public string NewSetting { get; set; } = "new";

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    private class SettingsWithLongMigrateFromMethod : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("Renamed setting")]
        [MigrateFrom("OldSetting", Chars256)]
        public string NewSetting { get; set; } = "new";

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    private class SettingsWithLongDataGridColumn : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("Grid setting")]
        public List<LongColumnItem> GridSetting { get; set; } = [];

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    public class LongColumnItem
    {
        public string xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx { get; set; } = "";
    }

    #endregion
}

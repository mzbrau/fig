using System.Collections.Generic;
using Fig.Client;
using Fig.Client.Attributes;
using Fig.Client.Description;
using Fig.Client.Enums;
using Fig.Client.EnvironmentVariables;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Fig.Integration.Test.Client;

public class TestSettings : SettingsBase
{
    public TestSettings()
    {
    }

    internal TestSettings(ISettingDefinitionFactory settingDefinitionFactory,
        IDescriptionProvider descriptionProvider,
        IEnvironmentVariableReader environmentVariableReader)
        : base(settingDefinitionFactory, descriptionProvider, environmentVariableReader)
    {
    }

    public string ClientName => "TestSettings";
    public override string ClientDescription => "Test Settings for the integration tests";

    [Setting("This is a test setting")]
    [Validation(@"(.*[a-z]){3,}", "Must have at least 3 characters")]
    [Group("My Group")]
    [Secret]
    public string StringSetting { get; set; } = "test";

    [Setting("This is an int setting")]
    [Fig.Client.Attributes.Category("Test", CategoryColor.Red)]
    [DisplayScript("if (IntSetting.Value == 4) { IntSetting.IsValid = true } else { IntSetting.IsValid = false }")]
    public int IntSetting { get; set; } = 4;

    [Setting("An Enum Setting")]
    [ValidValues(typeof(TestEnum))]
    public TestEnum EnumSetting { get; set; } = TestEnum.Item2;

    [Setting("A List")]
    public List<string>? ListSetting { get; set; }

    public string NotASetting { get; set; }

    public override void Validate(ILogger logger)
    {
        SetConfigurationErrorStatus(false);
    }
}
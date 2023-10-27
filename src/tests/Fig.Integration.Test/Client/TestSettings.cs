using System.Collections.Generic;
using Fig.Client;
using Fig.Client.Attributes;
using Fig.Client.Description;
using Fig.Client.Enums;
using Fig.Client.EnvironmentVariables;
using Fig.Common.NetStandard.IpAddress;
using Microsoft.Extensions.Logging;

namespace Fig.Integration.Test.Client;

public class TestSettings : SettingsBase
{
    public TestSettings()
    {
    }

    internal TestSettings(ISettingDefinitionFactory settingDefinitionFactory,
        IIpAddressResolver ipAddressResolver,
        IDescriptionProvider descriptionProvider,
        IEnvironmentVariableReader environmentVariableReader)
        : base(settingDefinitionFactory, ipAddressResolver, descriptionProvider, environmentVariableReader)
    {
    }

    public string ClientName => "TestSettings";
    public override string ClientDescription => "Test Settings for the integration tests";

    [Setting("This is a test setting", "test")]
    [Validation(@"(.*[a-z]){3,}", "Must have at least 3 characters")]
    [Group("My Group")]
    [Secret]
    [DisplayOrder(1)]
    public string StringSetting { get; set; } = null!;

    [Setting("This is an int setting", 4)]
    [DisplayOrder(2)]
    [Category("Test", CategoryColor.Red)]
    public int IntSetting { get; set; }

    [Setting("An Enum Setting", TestEnum.Item2)]
    [ValidValues(typeof(TestEnum))]
    public TestEnum EnumSetting { get; set; }

    [Setting("A List", null)]
    public List<string> ListSetting { get; set; }

    public string NotASetting { get; set; }

    public override void Validate(ILogger logger)
    {
        SetConfigurationErrorStatus(false);
    }
}
using System.Collections.Generic;
using Fig.Client;
using Fig.Client.Attributes;
using Fig.Client.SettingVerification;

namespace Fig.Integration.Test.Client;

public class TestSettings : SettingsBase
{
    public TestSettings()
    {
    }

    public TestSettings(ISettingDefinitionFactory settingDefinitionFactory, ISettingVerificationDecompiler settingVerificationDecompiler)
        : base(settingDefinitionFactory, settingVerificationDecompiler)
    {
    }

    public override string ClientName => "TestSettings";

    [Setting("This is a test setting", "test")]
    [Validation(@"(.*[a-z]){3,}", "Must have at least 3 characters")]
    [Group("My Group")]
    [Secret]
    [DisplayOrder(1)]
    public string StringSetting { get; set; } = null!;

    [Setting("This is an int setting", 4)]
    [DisplayOrder(2)]
    public int IntSetting { get; set; }

    [Setting("An Enum Setting", TestEnum.Item2)]
    [ValidValues(typeof(TestEnum))]
    public TestEnum EnumSetting { get; set; }

    [Setting("A List", null)]
    public List<string> ListSetting { get; set; }

    public string NotASetting { get; set; }
}
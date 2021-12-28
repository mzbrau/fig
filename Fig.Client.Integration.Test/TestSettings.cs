using Fig.Client.Attributes;
using Fig.Contracts.Settings;

namespace Fig.Client.Integration.Test;

public class TestSettings : SettingsBase
{
    public TestSettings()
    {
    }
    
    public TestSettings(ISettingDefinitionFactory settingDefinitionFactory, SettingsDataContract dataContract)
        : base(settingDefinitionFactory, dataContract)
    {
    }
    
    public override string ServiceName => "TestSettings";
    public override string ServiceSecret => "Secret String";
        
    [Setting]
    [DefaultValue("test")]
    [SettingDescription("This is a test setting")]
    [Validation(@"(.*[a-z]){3,}", "Must have at least 3 characters")]
    [FriendlyName("String Setting")]
    [Group("My Group")]
    [Secret]
    public string StringSetting { get; protected set; }
    
    [Setting]
    [DefaultValue(4)]
    [SettingDescription("This is a test int setting")]
    [FriendlyName("Int Setting")]
    public int IntSetting { get; protected set; }
}
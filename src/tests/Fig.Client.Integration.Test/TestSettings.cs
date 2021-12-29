using System;
using System.Collections.Generic;
using Fig.Client.Attributes;
using Fig.Contracts.Settings;
using NUnit.Framework;

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
    [DisplayOrder(1)]
    public string StringSetting { get; set; } = null!;

    [Setting]
    [DefaultValue(4)]
    [SettingDescription("This is an int setting")]
    [FriendlyName("Int Setting")]
    [DisplayOrder(2)]
    public int IntSetting { get; set; }
    
    [Setting]
    [DefaultValue(TestEnum.Item2)]
    [ValidValues(typeof(TestEnum))]
    [SettingDescription("An Enum Setting")]
    [FriendlyName("Enum Setting")]
    public TestEnum EnumSetting { get; set; }
    
    [Setting]
    [SettingDescription("A List")]
    [FriendlyName("List Setting")]
    public List<string> ListSetting { get; set; }
    
    public string NotASetting { get; set; }
}
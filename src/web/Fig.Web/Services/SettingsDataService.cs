using System.Collections.Immutable;
using System.ComponentModel;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Services;

public class SettingsDataService : ISettingsDataService
{
    public SettingsDataService()
    {
        Init();
    }

    public IList<SettingsDefinitionDataContract>? Services { get; private set; }

    private void Init()
    {
        Services = new List<SettingsDefinitionDataContract>()
        {
            new SettingsDefinitionDataContract()
            {
                ServiceName = "MyService1",
                Settings = new List<SettingDefinitionDataContract>()
                {
                    new SettingDefinitionDataContract()
                    {
                        Name = "StringSetting",
                        Description = "This is a string setting",
                        FriendlyName = "String Setting",
                        Value = "StringValue"
                    },
                    new SettingDefinitionDataContract()
                    {
                        Name = "StringSetting2",
                        Description = "This is a string setting 2",
                        FriendlyName = "String Setting 2",
                        Value = "StringValue2"
                    }
                }
            },
            new SettingsDefinitionDataContract()
            {
                ServiceName = "MyService2",
                Settings = new List<SettingDefinitionDataContract>()
                {
                    new SettingDefinitionDataContract()
                    {
                        Name = "StringSetting3",
                        Description = "This is a string setting 3",
                        FriendlyName = "String Setting 3",
                        Value = "StringValue3"
                    },
                    new SettingDefinitionDataContract()
                    {
                        Name = "StringSetting4",
                        Description = "This is a string setting 4",
                        FriendlyName = "String Setting 4",
                        Value = "StringValue4"
                    }
                }
            }
        };
    }
}
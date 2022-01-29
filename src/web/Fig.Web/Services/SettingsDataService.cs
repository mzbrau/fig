using Fig.Web.Models;

namespace Fig.Web.Services;

public class SettingsDataService : ISettingsDataService
{
    public SettingsDataService()
    {
        Init();
    }

    public IList<SettingsConfigurationModel>? Services { get; private set; }

    private void Init()
    {
        Services = new List<SettingsConfigurationModel>()
        {
            new SettingsConfigurationModel()
            {
                Name = "MyService1",
                DisplayName = "MyService1",
                Settings = new List<SettingConfigurationModel>()
                {
                    new StringSettingConfigurationModel()
                    {
                        Name = "StringSetting",
                        Description = "This is a string setting",
                        Value = "StringValue",
                        IsSecret = true,
                    },
                    new StringSettingConfigurationModel()
                    {
                        Name = "StringSetting2",
                        Description = "This is a string setting 2",
                        Value = "StringValue2"
                    },
                    new IntSettingConfigurationModel()
                    {
                        Name = "IntSetting",
                        Description = "This is int setting",
                        Value = 5
                    }
                }
            },
            new SettingsConfigurationModel()
            {
                Name = "MyService2",
                DisplayName = "MyService2",
                Settings = new List<SettingConfigurationModel>()
                {
                    new StringSettingConfigurationModel()
                    {
                        Name = "StringSetting3",
                        Description = "This is a string setting 3",
                        Value = "StringValue3"
                    },
                    new StringSettingConfigurationModel()
                    {
                        Name = "StringSetting4",
                        Description = "This is a string setting 4",
                        Value = "StringValue4"
                    }
                }
            }
        };
    }
}
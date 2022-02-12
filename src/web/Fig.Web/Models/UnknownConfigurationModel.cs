using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models;

public class UnknownConfigurationModel : SettingConfigurationModel<string>
{
    public UnknownConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent)
        : base(dataContract, parent)
    {
    }

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty)
    {
        return new UnknownConfigurationModel(DefinitionDataContract, parent);
    }
}
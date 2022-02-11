using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models;

public class UnknownConfigurationModel : SettingConfigurationModel<string>
{
    public UnknownConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent)
        : base(dataContract, parent)
    {
    }

    public override dynamic GetValue()
    {
        return "Not implemented";
    }

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty)
    {
        return new UnknownConfigurationModel(_definitionDataContract, parent);
    }
}
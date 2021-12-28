using System.Reflection;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.SettingTypes;

namespace Fig.Client
{
    public interface ISettingDefinitionFactory
    {
        ISettingDefinition Create(PropertyInfo settingProperty);
    }
}
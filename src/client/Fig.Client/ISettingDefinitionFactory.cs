using System.Reflection;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Client
{
    public interface ISettingDefinitionFactory
    {
        SettingDefinitionDataContract Create(PropertyInfo settingProperty, bool liveReload);
    }
}
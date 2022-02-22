using Fig.Web.Models.Setting;

namespace Fig.Web.Builders;

public interface ISettingGroupBuilder
{
    IEnumerable<SettingClientConfigurationModel> BuildGroups(IEnumerable<SettingClientConfigurationModel> clients);
}
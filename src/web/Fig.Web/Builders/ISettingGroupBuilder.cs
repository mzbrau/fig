using Fig.Web.Models;

namespace Fig.Web.Builders;

public interface ISettingGroupBuilder
{
    IEnumerable<SettingClientConfigurationModel> BuildGroups(IEnumerable<SettingClientConfigurationModel> clients);
}
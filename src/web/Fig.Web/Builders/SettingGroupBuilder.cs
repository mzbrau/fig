using Fig.Web.Models.Setting;

namespace Fig.Web.Builders;

public class SettingGroupBuilder : ISettingGroupBuilder
{
    public IEnumerable<SettingClientConfigurationModel> BuildGroups(
        IEnumerable<SettingClientConfigurationModel> clients)
    {
        var groupGrouping = ExtractGroups(clients);

        foreach (var group in groupGrouping)
        {
            var settingGroup = new SettingClientConfigurationModel(group.Key, null, true);

            settingGroup.Settings = CloneUniqueSettings(group, settingGroup);

            foreach (var setting in settingGroup.Settings)
            {
                var matches = group.Where(g => g.Name == setting.Name);
                setting.SetGroupManagedSettings(matches.ToList());
            }

            yield return settingGroup;
        }
    }

    private IEnumerable<IGrouping<string, ISetting>> ExtractGroups(
        IEnumerable<SettingClientConfigurationModel> clients)
    {
        return clients.Where(a => a.Instance == null)
            .SelectMany(a => a.Settings)
            .Where(a => !string.IsNullOrWhiteSpace(a.Group))
            .GroupBy(a => a.Group);
    }

    private List<ISetting> CloneUniqueSettings(
        IGrouping<string, ISetting> grouping,
        SettingClientConfigurationModel parent)
    {
        return grouping.DistinctBy(a => a.Name)
            .Select(s => s.Clone(parent, false))
            .ToList();
    }
}
using System.Text;
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
            var settingGroup = new SettingClientConfigurationModel(group.Key, CreateDescription(group), null, true);

            settingGroup.Settings = CloneUniqueSettings(group, settingGroup);

            foreach (var setting in settingGroup.Settings)
            {
                var matches = group.Where(g => g.Name == setting.Name);
                setting.SetGroupManagedSettings(matches.ToList());
            }

            yield return settingGroup;
        }
    }

    private string CreateDescription(IGrouping<string,ISetting> group)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# Setting Group: {group.Key}");
        builder.AppendLine();
        builder.AppendLine($"Group consists of {group.DistinctBy(a => a.Name).Count()} setting(s) used by the following clients:");
        foreach (var parent in group.Select(a => a.Parent).Select(a => a.DisplayName).Distinct().OrderBy(a => a))
        {
            builder.AppendLine($"- {parent}");
        }

        builder.AppendLine();
        builder.AppendLine("## Settings");
        builder.AppendLine();
        foreach (var setting in group.DistinctBy(a => a.Name))
        {
            builder.AppendLine($"### {setting.Name}");
            builder.AppendLine();
            builder.AppendLine(setting.Description.ToString());
            builder.AppendLine();
        }

        return builder.ToString();
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
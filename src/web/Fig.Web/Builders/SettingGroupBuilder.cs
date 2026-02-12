using System.Text;
using Fig.Common.NetStandard.Scripting;
using Fig.Web.Models.Setting;

namespace Fig.Web.Builders;

public class SettingGroupBuilder : ISettingGroupBuilder
{
    private readonly IScriptRunner _scriptRunner;

    public SettingGroupBuilder(IScriptRunner scriptRunner)
    {
        _scriptRunner = scriptRunner;
    }
    
    public IEnumerable<SettingClientConfigurationModel> BuildGroups(
        IEnumerable<SettingClientConfigurationModel> clients)
    {
        var groupGrouping = ExtractGroups(clients);

        foreach (var group in groupGrouping)
        {
            var settingsByLeaf = group.ToLookup(g => GetLeafName(g.Name), StringComparer.Ordinal);
            var settingGroup = new SettingClientConfigurationModel(group.Key, CreateDescription(group, settingsByLeaf), null, false, _scriptRunner, true);

            settingGroup.Settings = CloneUniqueSettings(settingsByLeaf, settingGroup);

            yield return settingGroup;
        }
    }

    private string CreateDescription(
        IGrouping<string,ISetting> group,
        ILookup<string, ISetting> settingsByLeaf)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# Setting Group: {group.Key}");
        builder.AppendLine();
        builder.AppendLine($"Group consists of {settingsByLeaf.Count} setting(s) used by the following clients:");
        foreach (var parent in group.Select(a => a.Parent).Select(a => a.DisplayName).Distinct().OrderBy(a => a))
        {
            builder.AppendLine($"- {parent}");
        }

        builder.AppendLine();
        builder.AppendLine("## Settings");
        builder.AppendLine();
        foreach (var setting in settingsByLeaf.Select(leafGroup => leafGroup.First()))
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
            .GroupBy(a => a.Group!);
    }

    private List<ISetting> CloneUniqueSettings(
        ILookup<string, ISetting> settingsByLeaf,
        SettingClientConfigurationModel parent)
    {
        var settings = new List<ISetting>(settingsByLeaf.Count);
        foreach (var leafGroup in settingsByLeaf)
        {
            var source = leafGroup.First();
            var cloned = source.Clone(parent, false, source.IsReadOnly);
            cloned.SetGroupManagedSettings(leafGroup.ToList());
            settings.Add(cloned);
        }

        return settings;
    }

    private static string GetLeafName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var parts = name.Split("->", StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? name.Trim() : parts[^1].Trim();
    }
}
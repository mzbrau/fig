using System.Collections.Generic;
using System.Linq;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Client;

internal static class SettingDefinitionWarnings
{
    internal sealed class HiddenCategoryHeadingWarning
    {
        public required string CategoryName { get; init; }
        public required string FirstSettingName { get; init; }
        public required IReadOnlyList<string> NonAdvancedSettingNames { get; init; }
    }

    internal static IEnumerable<HiddenCategoryHeadingWarning> GetHiddenCategoryHeadingWarnings(
        IReadOnlyList<SettingDefinitionDataContract> settings)
    {
        var orderedSettings = settings
            .OrderBy(s => s.DisplayOrder ?? int.MaxValue)
            .ToList();

        var categoryLeaders = new Dictionary<string, SettingDefinitionDataContract>();

        foreach (var setting in orderedSettings)
        {
            if (string.IsNullOrEmpty(setting.CategoryName))
            {
                continue;
            }

            if (!categoryLeaders.ContainsKey(setting.CategoryName))
            {
                categoryLeaders[setting.CategoryName] = setting;
            }
        }

        foreach (var categoryLeader in categoryLeaders)
        {
            var categoryName = categoryLeader.Key;
            var leader = categoryLeader.Value;

            if (leader.Heading?.Advanced != true)
            {
                continue;
            }

            var leaderDisplayOrder = leader.DisplayOrder ?? int.MaxValue;
            var nonAdvancedFollowers = orderedSettings
                .Where(s => s.CategoryName == categoryName
                            && (s.DisplayOrder ?? int.MaxValue) > leaderDisplayOrder
                            && !s.Advanced)
                .Select(s => s.Name)
                .ToList();

            if (nonAdvancedFollowers.Count == 0)
            {
                continue;
            }

            yield return new HiddenCategoryHeadingWarning
            {
                CategoryName = categoryName,
                FirstSettingName = leader.Name,
                NonAdvancedSettingNames = nonAdvancedFollowers
            };
        }
    }
}

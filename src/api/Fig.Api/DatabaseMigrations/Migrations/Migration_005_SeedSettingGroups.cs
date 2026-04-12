using Fig.Api.Datalayer.Repositories;
using Fig.Api.Utils;
using Fig.Contracts.SettingGroups;
using Fig.Datalayer.BusinessEntities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fig.Api.DatabaseMigrations.Migrations;

/// <summary>
/// Seeds setting groups from existing client group attributes.
/// The setting_groups table is created by NHibernate SchemaUpdate from the mapping;
/// this migration only populates initial data.
/// </summary>
public class Migration_005_SeedSettingGroups : IDatabaseMigration
{
    public int ExecutionNumber => 5;

    public string Description => "Seed setting groups from existing client group attributes";

    public string SqlServerScript => "SELECT 'Setting groups table created by NHibernate SchemaUpdate' as result;";

    public string SqliteScript => "SELECT 'Setting groups table created by NHibernate SchemaUpdate' as result;";

    public async Task? ExecuteCode(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Migration_005_SeedSettingGroups>>();
        var settingClientRepository = serviceProvider.GetRequiredService<ISettingClientRepository>();
        var settingGroupRepository = serviceProvider.GetRequiredService<ISettingGroupRepository>();

        logger.LogInformation("Starting seed of setting groups from existing client group attributes");

        try
        {
            var allClients = await settingClientRepository.GetAllClients(new ServiceUser(), upgradeLock: false, validateCode: false);

            // Collect all settings that have a Group attribute, across base clients only
            // (exclude instance overrides to match original SettingGroupBuilder behavior)
            var settingsWithGroups = allClients
                .Where(client => client.Instance == null)
                .SelectMany(client => (client.Settings ?? Enumerable.Empty<SettingBusinessEntity>())
                    .Where(s => !string.IsNullOrWhiteSpace(s.Group))
                    .Select(s => new
                    {
                        ClientName = client.Name,
                        SettingName = s.Name,
                        GroupName = s.Group!,
                        ValueType = s.ValueType?.FullName ?? "System.String"
                    }))
                .ToList();

            if (!settingsWithGroups.Any())
            {
                logger.LogInformation("No settings with group attributes found. Skipping group seeding");
                return;
            }

            var groupsByName = settingsWithGroups.GroupBy(s => s.GroupName);

            foreach (var grouping in groupsByName)
            {
                var groupName = grouping.Key;

                var existing = await settingGroupRepository.GetGroupByName(groupName);
                if (existing != null)
                {
                    logger.LogInformation("Setting group '{GroupName}' already exists, skipping", groupName);
                    continue;
                }

                // Sub-group by leaf setting name (last part after "->")
                var settingsByLeaf = grouping.GroupBy(s => GetLeafName(s.SettingName));

                var groupedSettings = new List<GroupedSettingDataContract>();
                foreach (var leafGroup in settingsByLeaf)
                {
                    var first = leafGroup.First();
                    var sourceSettings = leafGroup
                        .Select(s => new SourceSettingDataContract(s.ClientName, s.SettingName))
                        .ToList();

                    groupedSettings.Add(new GroupedSettingDataContract(
                        first.SettingName, null, first.ValueType, sourceSettings));
                }

                var entity = new SettingGroupBusinessEntity
                {
                    Name = groupName,
                    GroupSettingsJson = JsonConvert.SerializeObject(groupedSettings),
                    CreatedAt = DateTime.UtcNow,
                    LastModifiedAt = DateTime.UtcNow,
                    LastModifiedBy = "Migration"
                };

                await settingGroupRepository.AddGroup(entity);
                logger.LogInformation("Created setting group '{GroupName}' with {Count} grouped settings",
                    groupName, groupedSettings.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed setting groups");
            throw;
        }
    }

    private static string GetLeafName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var parts = name.Split("->", StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? name.Trim() : parts[^1].Trim();
    }
}

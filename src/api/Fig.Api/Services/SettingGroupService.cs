using System.Diagnostics;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.ExtensionMethods;
using Fig.Api.Observability;
using Fig.Client.Abstractions.Data;
using Fig.Common.Constants;
using Fig.Contracts.Authentication;
using Fig.Contracts.SettingGroups;
using Fig.Datalayer.BusinessEntities;
using Newtonsoft.Json;

namespace Fig.Api.Services;

public class SettingGroupService : AuthenticatedService, ISettingGroupService
{
    private readonly ISettingGroupRepository _settingGroupRepository;
    private readonly ISettingClientRepository _settingClientRepository;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly IEventLogFactory _eventLogFactory;
    private readonly ILogger<SettingGroupService> _logger;

    public SettingGroupService(
        ISettingGroupRepository settingGroupRepository,
        ISettingClientRepository settingClientRepository,
        IEventLogRepository eventLogRepository,
        IEventLogFactory eventLogFactory,
        ILogger<SettingGroupService> logger)
    {
        _settingGroupRepository = settingGroupRepository;
        _settingClientRepository = settingClientRepository;
        _eventLogRepository = eventLogRepository;
        _eventLogFactory = eventLogFactory;
        _logger = logger;
    }

    public async Task<IEnumerable<SettingGroupDataContract>> GetAllGroups()
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity("GetAllGroups");
        var stopwatch = Stopwatch.StartNew();

        var entities = await _settingGroupRepository.GetAllGroups();
        if (!entities.Any())
        {
            activity?.SetTag("fig.api.group_count", 0);
            activity?.SetTag("fig.api.elapsed_ms", stopwatch.ElapsedMilliseconds);
            return Enumerable.Empty<SettingGroupDataContract>();
        }

        var inaccessibleKeys = await GetRegisteredInaccessibleKeys();
        var groups = entities
            .Select(ConvertToDataContract)
            .Select(g => FilterGroupForUser(g, AuthenticatedUser, inaccessibleKeys))
            .OfType<SettingGroupDataContract>()
            .ToList();

        activity?.SetTag("fig.api.group_count", groups.Count);
        activity?.SetTag("fig.api.elapsed_ms", stopwatch.ElapsedMilliseconds);
        return groups;
    }

    public async Task<SettingGroupDataContract> GetGroup(Guid id)
    {
        var entity = await _settingGroupRepository.GetGroup(id, forUpdate: false)
            ?? throw new KeyNotFoundException($"No setting group found with id {id}");
        var group = ConvertToDataContract(entity);
        var inaccessibleKeys = await GetRegisteredInaccessibleKeys();
        return FilterGroupForUser(group, AuthenticatedUser, inaccessibleKeys)
            ?? throw new KeyNotFoundException($"No setting group found with id {id}");
    }

    public async Task<SettingGroupDataContract> CreateGroup(SettingGroupDataContract group)
    {
        await ValidateGroupName(group.Name);
        ValidateGroupedSettings(group);

        var entity = new SettingGroupBusinessEntity
        {
            Name = group.Name,
            Description = group.Description,
            GroupSettingsJson = SerializeGroupedSettings(group.GroupedSettings),
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow,
            LastModifiedBy = AuthenticatedUser?.Username
        };

        var id = await _settingGroupRepository.AddGroup(entity);
        await _eventLogRepository.Add(_eventLogFactory.GroupCreated(group.Name, AuthenticatedUser?.Username));

        group.Id = id;
        group.CreatedAt = entity.CreatedAt;
        group.LastModifiedAt = entity.LastModifiedAt;
        group.LastModifiedBy = entity.LastModifiedBy;
        return group;
    }

    public async Task<SettingGroupDataContract> UpdateGroup(Guid id, SettingGroupDataContract group)
    {
        var entity = await _settingGroupRepository.GetGroup(id, forUpdate: true)
            ?? throw new KeyNotFoundException($"No setting group found with id {id}");

        // Check name uniqueness if changed
        if (!string.Equals(entity.Name, group.Name, StringComparison.Ordinal))
        {
            await ValidateGroupName(group.Name);
        }

        ValidateGroupedSettings(group);

        var originalJson = entity.GroupSettingsJson;
        entity.Name = group.Name;
        entity.Description = group.Description;
        entity.GroupSettingsJson = SerializeGroupedSettings(group.GroupedSettings);
        entity.LastModifiedAt = DateTime.UtcNow;
        entity.LastModifiedBy = AuthenticatedUser?.Username;

        await _settingGroupRepository.UpdateGroup(entity);
        await _eventLogRepository.Add(_eventLogFactory.GroupUpdated(
            group.Name, originalJson, entity.GroupSettingsJson, AuthenticatedUser?.Username));

        return ConvertToDataContract(entity);
    }

    public async Task DeleteGroup(Guid id)
    {
        var entity = await _settingGroupRepository.GetGroup(id, forUpdate: true)
            ?? throw new KeyNotFoundException($"No setting group found with id {id}");

        await _settingGroupRepository.DeleteGroup(entity);
        await _eventLogRepository.Add(_eventLogFactory.GroupDeleted(entity.Name, AuthenticatedUser?.Username));
    }

    public async Task RemoveClientFromGroups(string clientName)
    {
        var allGroups = await _settingGroupRepository.GetAllGroups();
        foreach (var entity in allGroups)
        {
            var groupedSettings = DeserializeGroupedSettings(entity.GroupSettingsJson);
            var modified = false;

            foreach (var gs in groupedSettings.ToList())
            {
                var removed = gs.SourceSettings.RemoveAll(s =>
                    string.Equals(s.ClientName, clientName, StringComparison.OrdinalIgnoreCase));
                if (removed > 0)
                    modified = true;

                if (gs.SourceSettings.Count == 0)
                {
                    groupedSettings.Remove(gs);
                    modified = true;
                }
            }

            if (!modified) continue;

            if (groupedSettings.Count == 0)
            {
                await _settingGroupRepository.DeleteGroup(entity);
                _logger.LogInformation("Deleted empty setting group '{GroupName}' after removing client '{ClientName}'",
                    entity.Name.Sanitize(), clientName.Sanitize());
            }
            else
            {
                entity.GroupSettingsJson = SerializeGroupedSettings(groupedSettings);
                entity.LastModifiedAt = DateTime.UtcNow;
                entity.LastModifiedBy = "System";
                await _settingGroupRepository.UpdateGroup(entity);
                _logger.LogInformation("Removed client '{ClientName}' references from setting group '{GroupName}'",
                    clientName.Sanitize(), entity.Name.Sanitize());
            }
        }
    }

    public async Task ValidateClientRegistrationGroups(string clientName, IEnumerable<string> registeredSettingNames)
    {
        var registeredSettingLookup = new HashSet<string>(
            registeredSettingNames.Where(name => !string.IsNullOrWhiteSpace(name)),
            StringComparer.Ordinal);

        var allGroups = await _settingGroupRepository.GetAllGroups();
        foreach (var entity in allGroups)
        {
            var groupedSettings = DeserializeGroupedSettings(entity.GroupSettingsJson);
            var modified = false;
            var removedSourceSettingCount = 0;

            foreach (var groupedSetting in groupedSettings.ToList())
            {
                var removed = groupedSetting.SourceSettings.RemoveAll(sourceSetting =>
                    string.Equals(sourceSetting.ClientName, clientName, StringComparison.OrdinalIgnoreCase) &&
                    !registeredSettingLookup.Contains(sourceSetting.SettingName));

                if (removed == 0)
                    continue;

                removedSourceSettingCount += removed;
                modified = true;

                if (groupedSetting.SourceSettings.Count == 0)
                {
                    groupedSettings.Remove(groupedSetting);
                }
            }

            if (!modified)
                continue;

            if (groupedSettings.Count == 0)
            {
                await _settingGroupRepository.DeleteGroup(entity);
                _logger.LogInformation(
                    "Deleted empty setting group '{GroupName}' after validating updated registration for client '{ClientName}'",
                    entity.Name.Sanitize(),
                    clientName.Sanitize());
                continue;
            }

            entity.GroupSettingsJson = SerializeGroupedSettings(groupedSettings);
            entity.LastModifiedAt = DateTime.UtcNow;
            entity.LastModifiedBy = "System";
            await _settingGroupRepository.UpdateGroup(entity);
            _logger.LogInformation(
                "Removed {RemovedCount} orphaned source setting reference(s) for client '{ClientName}' from setting group '{GroupName}'",
                removedSourceSettingCount,
                clientName.Sanitize(),
                entity.Name.Sanitize());
        }
    }

    public async Task HandleInitialRegistrationGroups(string clientName, 
        IEnumerable<(string SettingName, string GroupName, string ValueType)> settingsWithGroups)
    {
        var allGroups = await _settingGroupRepository.GetAllGroups();
        var groupsByName = allGroups.ToDictionary(g => g.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var (settingName, groupName, valueType) in settingsWithGroups)
        {
            if (groupsByName.TryGetValue(groupName, out var existingGroup))
            {
                var groupedSettings = DeserializeGroupedSettings(existingGroup.GroupSettingsJson);
                var leafName = GetLeafName(settingName);

                // Match by the leaf name of existing source settings so that renaming the
                // grouped setting's display name (gs.Name) does not break future registrations.
                var matchingGs = groupedSettings.FirstOrDefault(gs =>
                    gs.SourceSettings.Any(s =>
                        string.Equals(GetLeafName(s.SettingName), leafName, StringComparison.Ordinal)) ||
                    string.Equals(GetLeafName(gs.Name), leafName, StringComparison.Ordinal));

                if (matchingGs != null)
                {
                    // Add to existing grouped setting if not already there
                    if (!matchingGs.SourceSettings.Any(s =>
                        string.Equals(s.ClientName, clientName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(s.SettingName, settingName, StringComparison.Ordinal)))
                    {
                        matchingGs.SourceSettings.Add(new SourceSettingDataContract(clientName, settingName));
                    }
                }
                else
                {
                    // Create new grouped setting in existing group
                    groupedSettings.Add(new GroupedSettingDataContract(
                        settingName, null, valueType,
                        new List<SourceSettingDataContract> { new(clientName, settingName) }));
                }

                existingGroup.GroupSettingsJson = SerializeGroupedSettings(groupedSettings);
                existingGroup.LastModifiedAt = DateTime.UtcNow;
                existingGroup.LastModifiedBy = "System";
                await _settingGroupRepository.UpdateGroup(existingGroup);
            }
            else
            {
                // Create new group
                var newGroup = new SettingGroupBusinessEntity
                {
                    Name = groupName,
                    GroupSettingsJson = SerializeGroupedSettings(new List<GroupedSettingDataContract>
                    {
                        new(settingName, null, valueType,
                            new List<SourceSettingDataContract> { new(clientName, settingName) })
                    }),
                    CreatedAt = DateTime.UtcNow,
                    LastModifiedAt = DateTime.UtcNow,
                    LastModifiedBy = "System"
                };
                await _settingGroupRepository.AddGroup(newGroup);
                groupsByName[groupName] = newGroup;
            }
        }
    }

    private async Task ValidateGroupName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Group name cannot be empty.");

        var existing = await _settingGroupRepository.GetGroupByName(name);
        if (existing != null)
            throw new InvalidOperationException($"A setting group with the name '{name}' already exists.");
    }

    private static void ValidateGroupedSettings(SettingGroupDataContract group)
    {
        if (group.GroupedSettings == null)
            return;

        foreach (var gs in group.GroupedSettings)
        {
            if (string.IsNullOrWhiteSpace(gs.Name))
                throw new ArgumentException("Grouped setting name cannot be empty.");

            if (gs.SourceSettings == null || gs.SourceSettings.Count == 0)
                throw new ArgumentException($"Grouped setting '{gs.Name}' must contain at least one source setting.");

            // All source settings must specify client and setting name
            foreach (var ss in gs.SourceSettings)
            {
                if (string.IsNullOrWhiteSpace(ss.ClientName))
                    throw new ArgumentException("Source setting client name cannot be empty.");
                if (string.IsNullOrWhiteSpace(ss.SettingName))
                    throw new ArgumentException("Source setting name cannot be empty.");
            }
        }
    }

    private static SettingGroupDataContract ConvertToDataContract(SettingGroupBusinessEntity entity)
    {
        var groupedSettings = DeserializeGroupedSettings(entity.GroupSettingsJson);
        return new SettingGroupDataContract(entity.Id, entity.Name, entity.Description, groupedSettings)
        {
            CreatedAt = entity.CreatedAt,
            LastModifiedAt = entity.LastModifiedAt,
            LastModifiedBy = entity.LastModifiedBy
        };
    }

    private static List<GroupedSettingDataContract> DeserializeGroupedSettings(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<GroupedSettingDataContract>();

        return JsonConvert.DeserializeObject<List<GroupedSettingDataContract>>(json) 
            ?? new List<GroupedSettingDataContract>();
    }

    private static string SerializeGroupedSettings(List<GroupedSettingDataContract> groupedSettings)
    {
        return JsonConvert.SerializeObject(groupedSettings);
    }

    private static string GetLeafName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var parts = name.Split("->", StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? name.Trim() : parts[^1].Trim();
    }

    private async Task<HashSet<string>> GetRegisteredInaccessibleKeys()
    {
        // If the user is null or all classifications are permitted, no setting can be inaccessible
        // by classification, so skip the expensive client load/decrypt entirely.
        if (AuthenticatedUser is null ||
            Enum.GetValues<Classification>().All(c => AuthenticatedUser.AllowedClassifications.Contains(c)))
        {
            return new HashSet<string>();
        }

        var accessibleClients = await _settingClientRepository.GetAllClients(AuthenticatedUser);
        var inaccessibleKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var client in accessibleClients)
        {
            foreach (var setting in client.Settings.Where(s => !AuthenticatedUser.HasPermissionForClassification(s)))
            {
                inaccessibleKeys.Add(BuildSettingKey(client.Name, setting.Name));
            }
        }

        return inaccessibleKeys;
    }

    private static string BuildSettingKey(string clientName, string settingName) =>
        $"{clientName}:::{settingName}";

    private static SettingGroupDataContract? FilterGroupForUser(
        SettingGroupDataContract group,
        UserDataContract? user,
        HashSet<string> registeredInaccessibleKeys)
    {
        // Groups with no configured source settings are always shown as-is
        if (!group.GroupedSettings.Any())
            return group;

        var filteredGroupedSettings = group.GroupedSettings
            .Select(gs => FilterGroupedSettingForUser(gs, user, registeredInaccessibleKeys))
            .OfType<GroupedSettingDataContract>()
            .ToList();

        if (!filteredGroupedSettings.Any())
            return null;

        return new SettingGroupDataContract(group.Id, group.Name, group.Description, filteredGroupedSettings)
        {
            CreatedAt = group.CreatedAt,
            LastModifiedAt = group.LastModifiedAt,
            LastModifiedBy = group.LastModifiedBy
        };
    }

    private static GroupedSettingDataContract? FilterGroupedSettingForUser(
        GroupedSettingDataContract gs,
        UserDataContract? user,
        HashSet<string> registeredInaccessibleKeys)
    {
        var filteredSources = gs.SourceSettings
            .Where(s => IsSourceSettingAccessible(s, user, registeredInaccessibleKeys))
            .ToList();

        if (!filteredSources.Any())
            return null;

        if (filteredSources.Count == gs.SourceSettings.Count)
            return gs;

        return new GroupedSettingDataContract(gs.Name, gs.Description, gs.ValueType, filteredSources)
        {
            CategoryName = gs.CategoryName,
            CategoryColor = gs.CategoryColor
        };
    }

    private static bool IsSourceSettingAccessible(
        SourceSettingDataContract source,
        UserDataContract? user,
        HashSet<string> registeredInaccessibleKeys)
    {
        // Filter by client name regex
        if (user?.HasAccess(source.ClientName) != true)
            return false;

        // Filter by classification only for registered settings;
        // unregistered clients are kept (classification unknown)
        return !registeredInaccessibleKeys.Contains(BuildSettingKey(source.ClientName, source.SettingName));
    }
}

using Fig.Api.Datalayer.Repositories;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Utils;

public class SettingChangeRecorder : ISettingChangeRecorder
{
    private readonly IEventLogRepository _eventLogRepository;
    private readonly ISettingHistoryRepository _settingHistoryRepository;
    private readonly IEventLogFactory _eventLogFactory;

    public SettingChangeRecorder(IEventLogRepository eventLogRepository,
        ISettingHistoryRepository settingHistoryRepository,
        IEventLogFactory eventLogFactory)
    {
        _eventLogRepository = eventLogRepository;
        _settingHistoryRepository = settingHistoryRepository;
        _eventLogFactory = eventLogFactory;
    }
    
    public async Task RecordSettingChanges(List<ChangedSetting> changes, string? changeMessage,
        DateTime timeOfUpdate,
        SettingClientBusinessEntity client,
        string? user)
    {
        foreach (var change in changes)
        {
            await _eventLogRepository.Add(_eventLogFactory.SettingValueUpdate(client.Id,
                client.Name,
                client.Instance,
                change.Name,
                change.OriginalValue?.GetValue(),
                change.NewValue?.GetValue(),
                changeMessage,
                timeOfUpdate,
                user));

            if (change.SettingIsExternallyManaged)
            {
                await _eventLogRepository.Add(_eventLogFactory.ExternallyManagedSettingUpdated(client.Id,
                    client.Name,
                    client.Instance,
                    change.Name,
                    change.OriginalValue?.GetValue(),
                    change.NewValue?.GetValue(),
                    changeMessage,
                    timeOfUpdate,
                    user));
            }

            await _settingHistoryRepository.Add(new SettingValueBusinessEntity
            {
                ClientId = client.Id,
                SettingName = change.Name,
                Value = change.NewValue,
                ChangedAt = timeOfUpdate,
                ChangedBy = user ?? "Unknown",
                ChangeMessage = changeMessage
            });
        }
    }
}
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
    
    public void RecordSettingChanges(List<ChangedSetting> changes, SettingClientBusinessEntity client,
        string? instance, string? user)
    {
        foreach (var change in changes)
        {
            _eventLogRepository.Add(_eventLogFactory.SettingValueUpdate(client.Id,
                client.Name,
                instance,
                change.Name,
                change.OriginalValue?.GetValue(),
                change.NewValue?.GetValue(),
                user));

            _settingHistoryRepository.Add(new SettingValueBusinessEntity
            {
                ClientId = client.Id,
                SettingName = change.Name,
                Value = change.NewValue,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = user ?? "Unknown"
            });
        }
    }
}
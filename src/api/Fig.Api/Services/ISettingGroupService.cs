using Fig.Contracts.SettingGroups;

namespace Fig.Api.Services;

public interface ISettingGroupService : IAuthenticatedService
{
    Task<IEnumerable<SettingGroupDataContract>> GetAllGroups();
    
    Task<SettingGroupDataContract> GetGroup(Guid id);
    
    Task<SettingGroupDataContract> CreateGroup(SettingGroupDataContract group);
    
    Task<SettingGroupDataContract> UpdateGroup(Guid id, SettingGroupDataContract group);
    
    Task DeleteGroup(Guid id);
    
    Task RemoveClientFromGroups(string clientName);
    
    Task HandleInitialRegistrationGroups(string clientName, IEnumerable<(string SettingName, string GroupName, string ValueType)> settingsWithGroups);
}

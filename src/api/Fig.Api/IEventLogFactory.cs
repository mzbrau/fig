using Fig.Api.DataImport;
using Fig.Contracts.Authentication;
using Fig.Contracts.ImportExport;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api;

public interface IEventLogFactory
{
    void SetRequesterDetails(string? ipAddress, string? hostname);

    EventLogBusinessEntity InitialRegistration(Guid clientId, string clientName);

    EventLogBusinessEntity IdenticalRegistration(Guid clientId, string clientName);

    EventLogBusinessEntity UpdatedRegistration(Guid clientId, string clientName);

    EventLogBusinessEntity SettingValueUpdate(Guid clientId,
        string clientName,
        string? instance,
        string settingName,
        object originalValue,
        object newValue,
        UserDataContract authenticatedUser);

    EventLogBusinessEntity ClientDeleted(Guid clientId, string clientName, string? instance,
        UserDataContract? authenticatedUser);

    EventLogBusinessEntity InstanceOverrideCreated(Guid clientId, string clientName, string? instance,
        UserDataContract? authenticatedUser);

    EventLogBusinessEntity VerificationRun(Guid clientId, string clientName, string? instance, string verificationName,
        UserDataContract? authenticatedUser, bool succeeded);

    EventLogBusinessEntity SettingsRead(Guid clientId, string clientName, string? instance);

    EventLogBusinessEntity LogIn(UserBusinessEntity user);

    EventLogBusinessEntity NewUser(UserBusinessEntity user, UserDataContract? authenticatedUser);

    EventLogBusinessEntity UpdateUser(UserBusinessEntity user, string originalDetails, bool passwordUpdated,
        UserDataContract? authenticatedUser);

    EventLogBusinessEntity DeleteUser(UserBusinessEntity user, UserDataContract? authenticatedUser);

    EventLogBusinessEntity NewSession(ClientRunSessionBusinessEntity session, ClientStatusBusinessEntity client);

    EventLogBusinessEntity ExpiredSession(ClientRunSessionBusinessEntity session, ClientStatusBusinessEntity client);
    
    EventLogBusinessEntity DataExported(UserDataContract? user);

    EventLogBusinessEntity DataImportStarted(ImportType importType, ImportMode mode, UserDataContract? user);

    EventLogBusinessEntity DataImported(ImportType importType, ImportMode mode, int clientAddedCount, UserDataContract? user);

    EventLogBusinessEntity Imported(SettingClientBusinessEntity client, UserDataContract? user);
}
using System.Runtime.InteropServices.JavaScript;
using Fig.Api.DataImport;
using Fig.Contracts.Authentication;
using Fig.Contracts.Configuration;
using Fig.Contracts.Health;
using Fig.Contracts.ImportExport;
using Fig.Contracts.Settings;
using Fig.Contracts.Status;
using Fig.Contracts.WebHook;
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
        object? originalValue,
        object? newValue,
        string? message,
        DateTime timeOfUpdate,
        string? username);
    
    EventLogBusinessEntity ExternallyManagedSettingUpdated(Guid clientId,
        string clientName,
        string? instance,
        string settingName,
        object? originalValue,
        object? newValue,
        string? message,
        DateTime timeOfUpdate,
        string? username);

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
    
    EventLogBusinessEntity DataExported(UserDataContract? authenticatedUser);

    EventLogBusinessEntity DataImportStarted(ImportType importType, ImportMode mode, UserDataContract? authenticatedUser);

    EventLogBusinessEntity DataImported(ImportType importType, ImportMode mode, int clientAddedCount, UserDataContract? authenticatedUser);

    EventLogBusinessEntity DataImportFailed(ImportType importType, ImportMode mode, UserDataContract? authenticatedUser, string errorMessage);

    EventLogBusinessEntity Imported(SettingClientBusinessEntity client, UserDataContract? authenticatedUser);

    EventLogBusinessEntity ConfigurationChanged(FigConfigurationDataContract before,
        FigConfigurationDataContract after, UserDataContract? authenticatedUser);

    EventLogBusinessEntity ConfigurationErrorStatusChanged(ClientStatusBusinessEntity clientStatusBusinessEntity,
        StatusRequestDataContract statusRequest);

    EventLogBusinessEntity ConfigurationError(ClientStatusBusinessEntity clientStatusBusinessEntity, string configurationError);
    
    EventLogBusinessEntity DeferredImportRegistered(ImportType dataImportType, ImportMode importMode, int deferredClientsCount, UserDataContract? authenticatedUser);
    
    EventLogBusinessEntity DeferredImportApplied(string name, string? instance);
    
    EventLogBusinessEntity WebHookSent(WebHookType webHookType, WebHookClientBusinessEntity webHookClient, string wasSuccessful);

    EventLogBusinessEntity ClientSecretChanged(Guid clientId, string clientName, string? instance,
        UserDataContract? authenticatedUser, DateTime oldSecretExpiry);

    EventLogBusinessEntity LiveReloadChange(ClientRunSessionBusinessEntity runSession, bool originalValue, UserDataContract? authenticatedUser);
    
    EventLogBusinessEntity RestartRequested(ClientRunSessionBusinessEntity runSession, UserDataContract? authenticatedUser);
    
    EventLogBusinessEntity CheckpointCreated(string message);
    
    EventLogBusinessEntity CheckPointApplied(UserDataContract? authenticatedUser, CheckPointBusinessEntity checkPoint);
    
    EventLogBusinessEntity NoteAddedToCheckPoint(UserDataContract? authenticatedUser, CheckPointBusinessEntity checkPoint);
    
    EventLogBusinessEntity ChangesScheduled(string clientName,
        string? instance,
        string? authenticatedUsername,
        SettingValueUpdatesDataContract updatedSettings,
        DateTime scheduleTimeUtc,
        bool isRevert,
        bool isReschedule);

    EventLogBusinessEntity ScheduledChangesDeleted(string clientName, string? instance, string? requestingUser, SettingValueUpdatesDataContract changeSet, DateTime executeAtUtc);
    
    EventLogBusinessEntity HealthStatusChanged(ClientRunSessionBusinessEntity session,
        ClientStatusBusinessEntity client, HealthDataContract healthDetails, FigHealthStatus oldStatus);
}
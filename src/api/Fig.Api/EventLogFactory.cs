using System.Globalization;
using Fig.Api.Constants;
using Fig.Api.Converters;
using Fig.Api.DataImport;
using Fig.Api.ExtensionMethods;
using Fig.Contracts.Authentication;
using Fig.Contracts.Configuration;
using Fig.Contracts.Health;
using Fig.Contracts.ImportExport;
using Fig.Contracts.Settings;
using Fig.Contracts.WebHook;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api;

public class EventLogFactory : IEventLogFactory
{
    private readonly IValueToStringConverter _valueToStringConverter;
    private string? _requesterHostname;
    private string? _requestIpAddress;

    public EventLogFactory(IValueToStringConverter valueToStringConverter)
    {
        _valueToStringConverter = valueToStringConverter;
    }

    public void SetRequesterDetails(string? ipAddress, string? hostname)
    {
        _requestIpAddress = ipAddress;
        _requesterHostname = hostname;
    }

    public string? GetRequestIpAddress() => _requestIpAddress;
    
    public string? GetRequestHostname() => _requesterHostname;

    public EventLogBusinessEntity InitialRegistration(Guid clientId, string clientName)
    {
        return Create(EventMessage.InitialRegistration, clientId, clientName);
    }

    public EventLogBusinessEntity IdenticalRegistration(Guid clientId, string clientName)
    {
        return Create(EventMessage.RegistrationNoChange, clientId, clientName);
    }

    public EventLogBusinessEntity UpdatedRegistration(Guid clientId, string clientName)
    {
        return Create(EventMessage.RegistrationWithChange, clientId, clientName);
    }

    public EventLogBusinessEntity SettingValueUpdate(Guid clientId, string clientName, string? instance,
        string settingName,
        object? originalValue, object? newValue, string? message, DateTime timeOfUpdate, string? username)
    {
        return Create(EventMessage.SettingValueUpdated,
            clientId,
            clientName,
            instance,
            settingName,
            _valueToStringConverter.Convert(originalValue),
            _valueToStringConverter.Convert(newValue),
            message,
            username ?? "Unknown",
            timeOfEvent: timeOfUpdate);
    }
    
    public EventLogBusinessEntity ExternallyManagedSettingUpdated(Guid clientId, string clientName, string? instance,
        string settingName,
        object? originalValue, object? newValue, string? message, DateTime timeOfUpdate, string? username)
    {
        return Create(EventMessage.ExternallyManagedSettingUpdatedByUser,
            clientId,
            clientName,
            instance,
            settingName,
            _valueToStringConverter.Convert(originalValue),
            _valueToStringConverter.Convert(newValue),
            message,
            username ?? "Unknown",
            timeOfEvent: timeOfUpdate);
    }

    public EventLogBusinessEntity ClientDeleted(Guid clientId, string clientName, string? instance,
        UserDataContract? authenticatedUser)
    {
        return Create(EventMessage.ClientDeleted, clientId, clientName, instance,
            authenticatedUsername: authenticatedUser?.Username);
    }

    public EventLogBusinessEntity
        InstanceOverrideCreated(Guid clientId, string clientName, string? instance, UserDataContract? authenticatedUser)
    {
        return Create(EventMessage.ClientInstanceCreated, clientId, clientName, instance,
            authenticatedUsername: authenticatedUser?.Username);
    }

    public EventLogBusinessEntity SettingsRead(Guid clientId, string clientName, string? instance)
    {
        return Create(EventMessage.SettingsRead, clientId, clientName, instance);
    }

    public EventLogBusinessEntity LogIn(UserBusinessEntity user)
    {
        return Create(EventMessage.Login, authenticatedUsername: user.Username);
    }

    public EventLogBusinessEntity LogInFailed(string? username, string? failureReason)
    {
        return Create(EventMessage.LoginFailed, 
            authenticatedUsername: username,
            message: failureReason);
    }

    public EventLogBusinessEntity NewUser(UserBusinessEntity user, UserDataContract? authenticatedUser)
    {
        return Create(EventMessage.UserCreated, newValue: user.Details(),
            authenticatedUsername: authenticatedUser?.Username);
    }

    public EventLogBusinessEntity UpdateUser(UserBusinessEntity user, string originalDetails, bool passwordUpdated,
        UserDataContract? authenticatedUser)
    {
        return Create(passwordUpdated ? EventMessage.PasswordUpdated : EventMessage.UserUpdated,
            originalValue: originalDetails,
            newValue: user.Details(),
            authenticatedUsername: authenticatedUser?.Username);
    }

    public EventLogBusinessEntity DeleteUser(UserBusinessEntity user, UserDataContract? authenticatedUser)
    {
        return Create(EventMessage.UserDeleted, originalValue: user.Details(),
            authenticatedUsername: authenticatedUser?.Username);
    }

    public EventLogBusinessEntity NewSession(ClientRunSessionBusinessEntity session, ClientStatusBusinessEntity client)
    {
        return Create(EventMessage.NewSession,
            client.Id,
            client.Name,
            client.Instance,
            newValue:
            $"{session.Hostname ?? EventMessage.UnknownHostname} ({session.IpAddress ?? EventMessage.UnknownIp})");
    }

    public EventLogBusinessEntity ExpiredSession(ClientRunSessionBusinessEntity session,
        ClientStatusBusinessEntity client)
    {
        return Create(EventMessage.ExpiredSession,
            client.Id,
            client.Name,
            client.Instance,
            originalValue: $"{session.Hostname} ({session.IpAddress}) up time:{(DateTime.UtcNow - session.StartTimeUtc).TotalSeconds}s");
    }

    public EventLogBusinessEntity DataExported(UserDataContract? authenticatedUser)
    {
        return Create(EventMessage.DataExported, authenticatedUsername: authenticatedUser?.Username);
    }

    public EventLogBusinessEntity DataImportStarted(ImportType importType, ImportMode mode, UserDataContract? authenticatedUser)
    {
        return Create(EventMessage.DataImportStarted, message: $"Mode:{mode}, Type:{importType}", authenticatedUsername: authenticatedUser?.Username);
    }

    public EventLogBusinessEntity DataImported(ImportType importType, ImportMode mode, int clientAddedCount, UserDataContract? authenticatedUser)
    {
        return Create(EventMessage.DataImported, message: $"Mode:{mode}, Type:{importType}, Imported {clientAddedCount} clients", authenticatedUsername: authenticatedUser?.Username);
    }

    public EventLogBusinessEntity DataImportFailed(ImportType importType, ImportMode mode, UserDataContract? authenticatedUser, string errorMessage)
    {
        return Create(EventMessage.DataImportFailed, message: $"Mode:{mode}, Type:{importType}, Message {errorMessage}", authenticatedUsername: authenticatedUser?.Username);
    }

    public EventLogBusinessEntity Imported(SettingClientBusinessEntity client, UserDataContract? authenticatedUser)
    {
        return Create(EventMessage.ClientImported, client.Id, client.Name, client.Instance, authenticatedUsername: authenticatedUser?.Username);
    }

    public EventLogBusinessEntity ConfigurationChanged(FigConfigurationDataContract before, FigConfigurationDataContract after,
        UserDataContract? authenticatedUser)
    {
        return Create(EventMessage.ConfigurationChanged,
            originalValue: Convert.ToString(before,
                CultureInfo.InvariantCulture),
            newValue: Convert.ToString(after,
                CultureInfo.InvariantCulture),
            authenticatedUsername: authenticatedUser?.Username);
    }

    public EventLogBusinessEntity DeferredImportRegistered(ImportType importType, ImportMode mode,
        int deferredClientsCount, UserDataContract? authenticatedUser)
    {
        return Create(EventMessage.DeferredImportRegistered,
            message: $"Mode:{mode}, Type:{importType}, Registered {deferredClientsCount} deferred imports",
            authenticatedUsername: authenticatedUser?.Username);
    }

    public EventLogBusinessEntity DeferredImportApplied(string name, string? instance)
    {
        return Create(EventMessage.DeferredImportApplied, clientName: name, instance: instance);
    }

    public EventLogBusinessEntity WebHookSent(WebHookType webHookType, WebHookClientBusinessEntity webHookClient,
        string result)
    {
        return Create(EventMessage.WebHookSent, originalValue: result,
            message: $"{webHookType} webhook was sent to base address {webHookClient.BaseUri}");
    }

    public EventLogBusinessEntity ClientSecretChanged(Guid clientId, string clientName, string? instance,
        UserDataContract? authenticatedUser, DateTime oldSecretExpiry)
    {
        return Create(EventMessage.ClientSecretChanged, clientId, clientName, instance,
            message: $"Old secret expires {oldSecretExpiry:u} UTC",
            authenticatedUsername: authenticatedUser?.Username);
    }

    public EventLogBusinessEntity LiveReloadChange(ClientRunSessionBusinessEntity runSession, 
        bool originalValue, UserDataContract? authenticatedUser)
    {
        return Create(EventMessage.LiveReloadChanged, 
            originalValue: originalValue.ToString(), 
            newValue: runSession.LiveReload.ToString(),
            authenticatedUsername: authenticatedUser?.Username);
    }

    public EventLogBusinessEntity RestartRequested(ClientRunSessionBusinessEntity runSession, UserDataContract? authenticatedUser)
    {
        return Create(EventMessage.RestartRequested, 
            authenticatedUsername: authenticatedUser?.Username);
    }

    public EventLogBusinessEntity CheckpointCreated(string message)
    {
        return Create(EventMessage.CheckPointCreated, 
            message: message);
    }

    public EventLogBusinessEntity CheckPointApplied(UserDataContract? authenticatedUser, CheckPointBusinessEntity checkPoint)
    {
        return Create(EventMessage.CheckPointApplied,
            authenticatedUsername: authenticatedUser?.Username,
            message: $"Applied from {checkPoint.Timestamp:u}");
    }

    public EventLogBusinessEntity NoteAddedToCheckPoint(UserDataContract? authenticatedUser, CheckPointBusinessEntity checkPoint)
    {
        return Create(EventMessage.NoteAddedToCheckPoint,
            authenticatedUsername: authenticatedUser?.Username,
            message: $"Note '{checkPoint.Note}' added to checkpoint from {checkPoint.Timestamp:u}");
    }

    public EventLogBusinessEntity ChangesScheduled(string clientName, string? instance, string? authenticatedUsername,
        SettingValueUpdatesDataContract updatedSettings, DateTime scheduledAtUtc, bool isRevert, bool isReschedule)
    {
        var revertMessage = isRevert ? "to be reverted" : "for";
        var scheduled = isReschedule ? "rescheduled" : "scheduled";
        
        return Create(EventMessage.ChangesScheduled,
            authenticatedUsername: authenticatedUsername,
            clientName: clientName,
            instance: instance,
            message: $"Changes to {updatedSettings.ValueUpdates.Count()} setting(s) {scheduled} {revertMessage} {scheduledAtUtc:u} UTC");
    }

    public EventLogBusinessEntity ScheduledChangesDeleted(string clientName, string? instance, string? requestingUser,
        SettingValueUpdatesDataContract changeSet, DateTime executeAtUtc)
    {
        return Create(EventMessage.ScheduledChangesDeleted,
            authenticatedUsername: requestingUser,
            clientName: clientName,
            instance: instance,
            message: $"Changes to {changeSet.ValueUpdates.Count()} setting(s) that were scheduled to be applied {executeAtUtc:u} UTC were deleted and will no longer be applied.");
    }

    public EventLogBusinessEntity HealthStatusChanged(ClientRunSessionBusinessEntity session, ClientStatusBusinessEntity client,
        HealthDataContract healthDetails, FigHealthStatus oldStatus)
    {
        return Create(EventMessage.HealthStatusChanged,
            client.Id,
            client.Name,
            client.Instance,
            originalValue: oldStatus.ToString(),
            newValue: session.HealthStatus.ToString(),
            message: healthDetails.Summary(),
            authenticatedUsername: session.RunningUser);
    }

    public EventLogBusinessEntity CustomActionsRemoved(string clientName, IEnumerable<string> removedActions)
    {
        return Create(EventMessage.CustomActionsRemoved, 
            clientName: clientName,
            message: $"Removed custom actions: {string.Join(", ", removedActions)}");
    }

    public EventLogBusinessEntity CustomActionAdded(string clientName, string customActionName)
    {
        return Create(EventMessage.CustomActionAdded, 
            clientName: clientName,
            newValue: customActionName);
    }

    public EventLogBusinessEntity CustomActionUpdated(string clientName, string customActionName)
    {
        return Create(EventMessage.CustomActionUpdated, 
            clientName: clientName,
            newValue: customActionName);
    }

    public EventLogBusinessEntity CustomActionExecutionRequested(string clientName, string customActionName,
        UserDataContract? authenticatedUser, Guid? runSessionId)
    {
        return Create(EventMessage.CustomActionExecutionRequested, 
            clientName: clientName,
            newValue: customActionName,
            message: runSessionId.HasValue ? $"Requested for run session: {runSessionId}" : "Requested for auto-selected instance",
            authenticatedUsername: authenticatedUser?.Username);
    }

    public EventLogBusinessEntity CustomActionExecutionCompleted(string clientName, string customActionName, bool succeeded)
    {
        return Create(EventMessage.CustomActionExecutionCompleted, 
            clientName: clientName,
            newValue: customActionName ,
            message: $"Custom action {customActionName} execution completed {(succeeded ? "successfully" : "with errors")}");
    }

    public EventLogBusinessEntity InvalidClientSecretAttempt(string clientName, string action, string? requestIpAddress,
        string? requesterHostname)
    {
        return Create(EventMessage.InvalidClientSecretAttempt, 
            clientName: clientName,
            message: $"Invalid client secret attempt to {action} from {requesterHostname ?? EventMessage.UnknownHostname} ({requestIpAddress ?? EventMessage.UnknownIp})");
    }

    private EventLogBusinessEntity Create(string eventType,
        Guid? clientId = null,
        string? clientName = null,
        string? instance = null,
        string? settingName = null,
        string? originalValue = null,
        string? newValue = null,
        string? message = null,
        string? authenticatedUsername = null,
        DateTime? timeOfEvent = null)
    {
        return new EventLogBusinessEntity
        {
            Timestamp = timeOfEvent ?? DateTime.UtcNow,
            ClientId = clientId,
            ClientName = clientName,
            SettingName = settingName,
            EventType = eventType,
            OriginalValue = originalValue,
            NewValue = newValue,
            Instance = instance,
            AuthenticatedUser = authenticatedUsername,
            Message = message,
            IpAddress = _requestIpAddress,
            Hostname = _requesterHostname
        };
    }
}
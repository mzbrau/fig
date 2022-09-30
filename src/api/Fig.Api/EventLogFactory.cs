using Fig.Api.Constants;
using Fig.Api.Converters;
using Fig.Api.DataImport;
using Fig.Api.ExtensionMethods;
using Fig.Contracts.Authentication;
using Fig.Contracts.Configuration;
using Fig.Contracts.ImportExport;
using Fig.Contracts.Status;
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
        object originalValue, object newValue, UserDataContract? authenticatedUser)
    {
        return Create(EventMessage.SettingValueUpdated,
            clientId,
            clientName,
            instance,
            settingName,
            _valueToStringConverter.Convert(originalValue),
            _valueToStringConverter.Convert(newValue),
            authenticatedUser?.Username ?? "Unknown");
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

    public EventLogBusinessEntity VerificationRun(Guid clientId, string clientName, string? instance,
        string verificationName,
        UserDataContract? authenticatedUser, bool succeeded)
    {
        return Create(EventMessage.SettingVerificationRun, clientId, clientName, instance,
            verificationName: verificationName,
            authenticatedUsername: authenticatedUser?.Username, newValue: $"Result: {(succeeded ? "Pass" : "Fail")}");
    }

    public EventLogBusinessEntity SettingsRead(Guid clientId, string clientName, string? instance)
    {
        return Create(EventMessage.SettingsRead, clientId, clientName, instance);
    }

    public EventLogBusinessEntity LogIn(UserBusinessEntity user)
    {
        return Create(EventMessage.Login, authenticatedUsername: user.Username);
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
            originalValue: $"{session.Hostname} ({session.IpAddress}) up time:{session.UptimeSeconds}s");
    }

    public EventLogBusinessEntity DataExported(UserDataContract? authenticatedUser, bool decryptSecrets)
    {
        return Create(EventMessage.DataExported, newValue:$"decrypt secrets: {decryptSecrets}", authenticatedUsername: authenticatedUser?.Username);
    }

    public EventLogBusinessEntity DataImportStarted(ImportType importType, ImportMode mode, UserDataContract? authenticatedUser)
    {
        return Create(EventMessage.DataImportStarted, newValue: $"Mode:{mode}, Type:{importType}", authenticatedUsername: authenticatedUser?.Username);
    }

    public EventLogBusinessEntity DataImported(ImportType importType, ImportMode mode, int clientAddedCount, UserDataContract? authenticatedUser)
    {
        return Create(EventMessage.DataImported, newValue: $"Mode:{mode}, Type:{importType}, Imported {clientAddedCount} clients", authenticatedUsername: authenticatedUser?.Username);
    }

    public EventLogBusinessEntity Imported(SettingClientBusinessEntity client, UserDataContract? authenticatedUser)
    {
        return Create(EventMessage.ClientImported, client.Id, client.Name, client.Instance, authenticatedUsername: authenticatedUser?.Username);
    }

    public EventLogBusinessEntity ConfigurationChanged(FigConfigurationDataContract before, FigConfigurationDataContract after,
        UserDataContract? authenticatedUser)
    {
        return Create(EventMessage.ConfigurationChanged, originalValue: before.ToString(), newValue: after.ToString(), authenticatedUsername: authenticatedUser?.Username);
    }

    public EventLogBusinessEntity ConfigurationErrorStatusChanged(ClientStatusBusinessEntity clientStatus,
        StatusRequestDataContract statusRequest)
    {
        var eventType = statusRequest.HasConfigurationError
            ? EventMessage.HasConfigurationError
            : EventMessage.ConfigurationErrorCleared;
        return Create(eventType,
            clientStatus.Id,
            clientStatus.Name,
            clientStatus.Instance,
            null,
            (!statusRequest.HasConfigurationError).ToString(),
            statusRequest.HasConfigurationError.ToString());
    }

    public EventLogBusinessEntity ConfigurationError(ClientStatusBusinessEntity clientStatus, string configurationError)
    {
        return Create(EventMessage.ConfigurationError,
            clientStatus.Id,
            clientStatus.Name,
            clientStatus.Instance,
            null,
            null,
            configurationError);
    }

    public EventLogBusinessEntity DeferredImportRegistered(ImportType importType, ImportMode mode,
        int deferredClientsCount, UserDataContract? authenticatedUser)
    {
        return Create(EventMessage.DeferredImportRegistered, newValue: $"Mode:{mode}, Type:{importType}, Registered {deferredClientsCount} deferred imports", authenticatedUsername: authenticatedUser?.Username);
    }

    public EventLogBusinessEntity DeferredImportApplied(string name, string? instance)
    {
        return Create(EventMessage.DeferredImportApplied, clientName: name, instance: instance);
    }

    private EventLogBusinessEntity Create(string eventType,
        Guid? clientId = null,
        string? clientName = null,
        string? instance = null,
        string? settingName = null,
        string? originalValue = null,
        string? newValue = null,
        string? authenticatedUsername = null,
        string? verificationName = null)
    {
        return new EventLogBusinessEntity
        {
            Timestamp = DateTime.UtcNow,
            ClientId = clientId,
            ClientName = clientName,
            SettingName = settingName,
            EventType = eventType,
            OriginalValue = originalValue,
            NewValue = newValue,
            Instance = instance,
            AuthenticatedUser = authenticatedUsername,
            VerificationName = verificationName,
            IpAddress = _requestIpAddress,
            Hostname = _requesterHostname
        };
    }
}
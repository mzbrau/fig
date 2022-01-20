using Fig.Api.ExtensionMethods;
using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api;

public class EventLogFactory : IEventLogFactory
{
    private string? _requesterHostname;
    private string? _requestIpAddress;

    public void SetRequesterDetails(string? ipAddress, string? hostname)
    {
        _requestIpAddress = ipAddress;
        _requesterHostname = hostname;
    }

    public EventLogBusinessEntity InitialRegistration(Guid clientId, string clientName)
    {
        return Create("Initial Registration", clientId, clientName);
    }

    public EventLogBusinessEntity IdenticalRegistration(Guid clientId, string clientName)
    {
        return Create("Registration - No Change", clientId, clientName);
    }

    public EventLogBusinessEntity UpdatedRegistration(Guid clientId, string clientName)
    {
        return Create("Registration - Definition Changed", clientId, clientName);
    }

    public EventLogBusinessEntity SettingValueUpdate(Guid clientId, string clientName, string? instance,
        string settingName,
        object originalValue, object newValue, UserDataContract authenticatedUser)
    {
        return Create("Setting value updated",
            clientId,
            clientName,
            instance,
            settingName,
            originalValue?.ToString(),
            newValue?.ToString(),
            authenticatedUser.Username);
    }

    public EventLogBusinessEntity ClientDeleted(Guid clientId, string clientName, string? instance,
        UserDataContract? authenticatedUser)
    {
        return Create("Setting Client Deleted", clientId, clientName, instance,
            authenticatedUsername: authenticatedUser?.Username);
    }

    public EventLogBusinessEntity
        InstanceOverrideCreated(Guid clientId, string clientName, string? instance, UserDataContract? authenticatedUser)
    {
        return Create("Client instance created", clientId, clientName, instance,
            authenticatedUsername: authenticatedUser?.Username);
    }

    public EventLogBusinessEntity VerificationRun(Guid clientId, string clientName, string? instance,
        string verificationName,
        UserDataContract? authenticatedUser, bool succeeded)
    {
        return Create("Setting verification run", clientId, clientName, instance, verificationName: verificationName,
            authenticatedUsername: authenticatedUser?.Username, newValue: $"Result: {(succeeded ? "Pass" : "Fail")}");
    }

    public EventLogBusinessEntity SettingsRead(Guid clientId, string clientName, string? instance)
    {
        return Create("Settings Read", clientId, clientName, instance);
    }

    public EventLogBusinessEntity LogIn(UserBusinessEntity user)
    {
        return Create("Login", authenticatedUsername: user.Username);
    }

    public EventLogBusinessEntity NewUser(UserBusinessEntity user, UserDataContract? authenticatedUser)
    {
        return Create("User created", newValue: $"{user.Username} ({user.FirstName} {user.LastName})",
            authenticatedUsername: authenticatedUser?.Username);
    }

    public EventLogBusinessEntity UpdateUser(UserBusinessEntity user, string originalDetails, bool passwordUpdated,
        UserDataContract? authenticatedUser)
    {
        return Create(passwordUpdated ? "Password Updated" : "User Updated", originalValue: originalDetails,
            newValue: user.Details(),
            authenticatedUsername: authenticatedUser?.Username);
    }

    public EventLogBusinessEntity DeleteUser(UserBusinessEntity user, UserDataContract? authenticatedUser)
    {
        return Create("User deleted", originalValue: user.Details(),
            authenticatedUsername: authenticatedUser?.Username);
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
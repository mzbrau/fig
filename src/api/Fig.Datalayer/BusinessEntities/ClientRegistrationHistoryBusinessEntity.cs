namespace Fig.Datalayer.BusinessEntities;

/// <summary>
/// Represents a historical record of a client registration event.
/// Captures the state of settings at the time of registration.
/// </summary>
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
public class ClientRegistrationHistoryBusinessEntity
{
    public virtual Guid Id { get; init; }

    public virtual DateTime RegistrationDateUtc { get; set; }

    public virtual string ClientName { get; set; } = string.Empty;

    public virtual string ClientVersion { get; set; } = string.Empty;

    /// <summary>
    /// JSON serialized list of setting names and their default values.
    /// Format: [{"Name": "SettingName", "DefaultValue": "value"}, ...]
    /// </summary>
    public virtual string SettingsJson { get; set; } = string.Empty;
}

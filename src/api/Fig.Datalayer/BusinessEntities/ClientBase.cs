namespace Fig.Datalayer.BusinessEntities;

public abstract class ClientBase
{
    public virtual Guid Id { get; init; }

    public virtual string Name { get; set; } = string.Empty;

    public virtual string ClientSecret { get; set; } = string.Empty;

    public virtual string? PreviousClientSecret { get; set; } = string.Empty;

    public virtual DateTime? PreviousClientSecretExpiryUtc { get; set; }

    public virtual string? Instance { get; set; }

    public virtual DateTime? LastRegistration { get; set; }

    public virtual DateTime? LastSettingValueUpdate { get; set; }
}
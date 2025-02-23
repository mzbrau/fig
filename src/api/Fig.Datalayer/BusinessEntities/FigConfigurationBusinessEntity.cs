namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
public class FigConfigurationBusinessEntity
{
    public virtual Guid Id { get; init; }
    
    public virtual bool AllowNewRegistrations { get; set; } = true;

    public virtual bool AllowUpdatedRegistrations { get; set; } = true;

    public virtual bool AllowFileImports { get; set; } = true;

    public virtual bool AllowOfflineSettings { get; set; } = true;

    public virtual bool AllowClientOverrides { get; set; } = true;

    public virtual string? ClientOverridesRegex { get; set; } = ".*";

    public virtual string? WebApplicationBaseAddress { get; set; } = "http://localhost:5050/";

    public virtual bool UseAzureKeyVault { get; set; } = false;
    
    public virtual string? AzureKeyVaultName { get; set; }
    
    public virtual double? PollIntervalOverride { get; set; }

    public virtual bool AllowDisplayScripts { get; set; } = false;

    public virtual bool EnableTimeMachine { get; set; } = true;
}
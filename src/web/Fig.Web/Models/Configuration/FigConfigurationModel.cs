namespace Fig.Web.Models.Configuration;

public class FigConfigurationModel
{
    public bool AllowNewRegistrations { get; set; }

    public bool AllowUpdatedRegistrations { get; set; }

    public bool AllowFileImports { get; set; }

    public bool AllowOfflineSettings { get; set; }

    public bool AllowClientOverrides { get; set; }
        
    public string? ClientOverridesRegex { get; set; }

    public string? WebApplicationBaseAddress { get; set; }

    public bool UseAzureKeyVault { get; set; }
    
    public string? AzureKeyVaultName { get; set; }
    
    public double? PollIntervalOverride { get; set; }
    
    public bool AllowDisplayScripts { get; set; }
    
    public bool EnableTimeMachine { get; set; }
    
    public int TimelineDurationDays { get; set; } = 60;

    public FigConfigurationModel Clone()
    {
        return new FigConfigurationModel
        {
            AllowNewRegistrations = AllowNewRegistrations,
            AllowUpdatedRegistrations = AllowUpdatedRegistrations,
            AllowFileImports = AllowFileImports,
            AllowOfflineSettings = AllowOfflineSettings,
            AllowClientOverrides = AllowClientOverrides,
            ClientOverridesRegex = ClientOverridesRegex,
            WebApplicationBaseAddress = WebApplicationBaseAddress,
            UseAzureKeyVault = UseAzureKeyVault,
            AzureKeyVaultName = AzureKeyVaultName,
            PollIntervalOverride = PollIntervalOverride,
            AllowDisplayScripts = AllowDisplayScripts,
            EnableTimeMachine = EnableTimeMachine,
            TimelineDurationDays = TimelineDurationDays
        };
    }

    public void Revert(FigConfigurationModel model)
    {
        AllowNewRegistrations = model.AllowNewRegistrations;
        AllowUpdatedRegistrations = model.AllowUpdatedRegistrations;
        AllowFileImports = model.AllowFileImports;
        AllowOfflineSettings = model.AllowOfflineSettings;
        AllowClientOverrides = model.AllowClientOverrides;
        ClientOverridesRegex = model.ClientOverridesRegex;
        WebApplicationBaseAddress = model.WebApplicationBaseAddress;
        UseAzureKeyVault = model.UseAzureKeyVault;
        AzureKeyVaultName = model.AzureKeyVaultName;
        PollIntervalOverride = model.PollIntervalOverride;
        AllowDisplayScripts = model.AllowDisplayScripts;
        EnableTimeMachine = model.EnableTimeMachine;
        TimelineDurationDays = model.TimelineDurationDays;
    }
}
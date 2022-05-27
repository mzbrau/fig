namespace Fig.Web.Models.Configuration;

public class FigConfigurationModel
{
    public bool AllowNewRegistrations { get; set; }

    public bool AllowUpdatedRegistrations { get; set; }

    public bool AllowFileImports { get; set; }

    public bool AllowOfflineSettings { get; set; }

    public bool AllowDynamicVerifications { get; set; }

    public FigConfigurationModel Clone()
    {
        return new FigConfigurationModel
        {
            AllowNewRegistrations = AllowNewRegistrations,
            AllowUpdatedRegistrations = AllowUpdatedRegistrations,
            AllowFileImports = AllowFileImports,
            AllowOfflineSettings = AllowOfflineSettings,
            AllowDynamicVerifications = AllowDynamicVerifications
        };
    }

    public void Revert(FigConfigurationModel model)
    {
        AllowNewRegistrations = model.AllowNewRegistrations;
        AllowUpdatedRegistrations = model.AllowUpdatedRegistrations;
        AllowFileImports = model.AllowFileImports;
        AllowOfflineSettings = model.AllowOfflineSettings;
        AllowDynamicVerifications = model.AllowDynamicVerifications;
    }
}
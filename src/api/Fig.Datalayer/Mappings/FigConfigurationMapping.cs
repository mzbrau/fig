using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.Constants;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class FigConfigurationMapping : ClassMapping<FigConfigurationBusinessEntity>
{
    public FigConfigurationMapping()
    {
        Table(Mapping.ConfigurationTable);
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.AllowNewRegistrations, x => x.Column("allow_new_registrations"));
        Property(x => x.AllowUpdatedRegistrations, x => x.Column("allow_updated_registrations"));
        Property(x => x.AllowFileImports, x => x.Column("allow_file_imports"));
        Property(x => x.AllowOfflineSettings, x => x.Column("allow_offline_settings"));
        Property(x => x.AllowClientOverrides, x => x.Column("allow_client_overrides"));
        Property(x => x.ClientOverridesRegex, x => x.Column("client_overrides_regex"));
        Property(x => x.WebApplicationBaseAddress, x => x.Column("web_application_base_address"));
        Property(x => x.UseAzureKeyVault, x => x.Column("use_azure_key_vault"));
        Property(x => x.AzureKeyVaultName, x => x.Column("azure_key_vault_name"));
        Property(x => x.PollIntervalOverride, x => x.Column("poll_interval_override"));
        Property(x => x.AllowDisplayScripts, x => x.Column("allow_display_scripts"));
        Property(x => x.EnableTimeMachine, x => x.Column("enable_time_machine"));
        Property(x => x.TimelineDurationDays, x => x.Column("timeline_duration_days"));
    }
}
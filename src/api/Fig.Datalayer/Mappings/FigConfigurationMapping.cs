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
        Property(x => x.TimeMachineCleanupDays, x => x.Column("time_machine_cleanup_days"));
        Property(x => x.EventLogsCleanupDays, x => x.Column("event_logs_cleanup_days"));
        Property(x => x.ApiStatusCleanupDays, x => x.Column("api_status_cleanup_days"));
        Property(x => x.SettingHistoryCleanupDays, x => x.Column("setting_history_cleanup_days"));
        Property(x => x.AllowMigrateFromMigrations, x => x.Column("allow_migrate_from_migrations"));
        Property(x => x.EnableFigAssistant, x => x.Column("enable_fig_assistant"));
        Property(x => x.FigAssistantEndpoint, x => x.Column("fig_assistant_endpoint"));
        Property(x => x.FigAssistantModel, x => x.Column("fig_assistant_model"));
        Property(x => x.FigAssistantAccessTokenEncrypted, x =>
        {
            x.Column("fig_assistant_access_token_encrypted");
            x.Length(2000);
        });
        Property(x => x.FigAssistantMaxToolIterations, x => x.Column("fig_assistant_max_tool_iterations"));
        Property(x => x.FigAssistantRequestTimeoutSeconds, x => x.Column("fig_assistant_request_timeout_seconds"));
    }
}
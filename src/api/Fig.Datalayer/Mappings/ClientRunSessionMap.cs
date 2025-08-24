using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.Constants;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class ClientRunSessionMap : ClassMapping<ClientRunSessionBusinessEntity>
{
    public ClientRunSessionMap()
    {
        Table(Mapping.RunSessionsTable);
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.RunSessionId, x => x.Column("run_session_id"));
        Property(x => x.LastSeen, x =>
        {
            x.Column("last_seen");
            x.Type(NHibernateUtil.UtcTicks);
        });
        Property(x => x.LiveReload, x => x.Column("live_reload"));
        Property(x => x.PollIntervalMs, x => x.Column("poll_interval_ms"));
        Property(x => x.StartTimeUtc, x =>
        {
            x.Column("start_time_utc");
            x.Type(NHibernateUtil.UtcTicks);
        });
        Property(x => x.LastSettingLoadUtc, x =>
        {
            x.Column("last_setting_load_utc");
            x.Type(NHibernateUtil.UtcTicks);
        });
        
        Property(x => x.IpAddress, x => x.Column("ip_address"));
        Property(x => x.Hostname, x => x.Column("hostname"));
        Property(x => x.FigVersion, x => x.Column("fig_version"));
        Property(x => x.ApplicationVersion, x => x.Column("app_version"));
        Property(x => x.OfflineSettingsEnabled, x => x.Column("offline_settings_enabled"));
        Property(x => x.RestartRequested, x => x.Column("restart_requested"));
        Property(x => x.RestartRequiredToApplySettings, x => x.Column("restart_required_to_apply_settings"));
        Property(x => x.SupportsRestart, x => x.Column("supports_restart"));
        Property(x => x.RunningUser, x => x.Column("running_user"));
        Property(x => x.MemoryUsageBytes, x => x.Column("memory_usage"));
        Property(x => x.HealthStatus, x => x.Column("health_status"));
        Property(x => x.HealthReportJson, x =>
        {
            x.Column("health_report_json");
            x.Type(NHibernateUtil.StringClob);
        });
        Property(x => x.InstanceName, x => x.Column("instance_name"));
    }
}
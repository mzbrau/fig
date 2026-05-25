using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.Constants;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class ApiSecretRotationStateMap : ClassMapping<ApiSecretRotationStateBusinessEntity>
{
    public ApiSecretRotationStateMap()
    {
        Table(Mapping.ApiSecretRotationStatesTable);
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.CurrentSecretFingerprint, x =>
        {
            x.Column("current_secret_fingerprint");
            x.Length(128);
        });
        Property(x => x.PreviousSecretFingerprint, x =>
        {
            x.Column("previous_secret_fingerprint");
            x.Length(128);
        });
        Property(x => x.Status, x =>
        {
            x.Column("status");
            x.Length(32);
        });
        Property(x => x.CreatedAtUtc, x =>
        {
            x.Column("created_at_utc");
            x.Type(NHibernateUtil.UtcTicks);
        });
        Property(x => x.UpdatedAtUtc, x =>
        {
            x.Column("updated_at_utc");
            x.Type(NHibernateUtil.UtcTicks);
        });
        Property(x => x.StartedAtUtc, x =>
        {
            x.Column("started_at_utc");
            x.Type(NHibernateUtil.UtcTicks);
        });
        Property(x => x.CompletedAtUtc, x =>
        {
            x.Column("completed_at_utc");
            x.Type(NHibernateUtil.UtcTicks);
        });
        Property(x => x.FailedAtUtc, x =>
        {
            x.Column("failed_at_utc");
            x.Type(NHibernateUtil.UtcTicks);
        });
        Property(x => x.StartedByHost, x =>
        {
            x.Column("started_by_host");
            x.Length(256);
        });
        Property(x => x.LastCompletedStage, x =>
        {
            x.Column("last_completed_stage");
            x.Length(128);
        });
        Property(x => x.ProcessedRecords, x => x.Column("processed_records"));
        Property(x => x.LastError, x =>
        {
            x.Column("last_error");
            x.Length(Mapping.NVarCharMax);
        });
    }
}

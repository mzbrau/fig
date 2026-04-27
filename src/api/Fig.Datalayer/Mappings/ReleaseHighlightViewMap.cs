using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.Constants;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class ReleaseHighlightViewMap : ClassMapping<ReleaseHighlightViewBusinessEntity>
{
    public ReleaseHighlightViewMap()
    {
        Table(Mapping.ReleaseHighlightViewsTable);
        Lazy(false);
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.UserId, x =>
        {
            x.Column("user_id");
            x.Index("release_highlight_views_user_index");
            x.UniqueKey("ux_release_highlight_views_user_version_feature");
        });
        Property(x => x.ReleaseVersion, x =>
        {
            x.Column("release_version");
            x.UniqueKey("ux_release_highlight_views_user_version_feature");
        });
        Property(x => x.FeatureKey, x =>
        {
            x.Column("feature_key");
            x.UniqueKey("ux_release_highlight_views_user_version_feature");
        });
        Property(x => x.ViewedAtUtc, x =>
        {
            x.Column("viewed_at_utc");
            x.Type(NHibernateUtil.UtcTicks);
        });
    }
}

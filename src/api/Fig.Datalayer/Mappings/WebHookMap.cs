using Fig.Datalayer.BusinessEntities;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class WebHookMap : ClassMapping<WebHookBusinessEntity>
{
    public WebHookMap()
    {
        Table("web_hook");
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.ClientId, x => x.Column("client_id"));
        Property(x => x.WebHookType, x =>
        {
            x.Column("web_hook_type");
            x.Index("web_hook_web_hook_type_index");
        });
        Property(x => x.ClientNameRegex, x => x.Column("client_name_regex"));
        Property(x => x.SettingNameRegex, x => x.Column("setting_name_regex"));
        Property(x => x.MinSessions, x => x.Column("min_sessions"));
    }
}
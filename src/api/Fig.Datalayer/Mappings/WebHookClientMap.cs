using Fig.Datalayer.BusinessEntities;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class WebHookClientMap : ClassMapping<WebHookClientBusinessEntity>
{
    public WebHookClientMap()
    {
        Table("web_hook_client");
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.Name, x => x.Column("name"));
        Property(x => x.BaseUri, x => x.Column("base_uri"));
        Property(x => x.Secret, x => x.Column("secret"));
    }
}
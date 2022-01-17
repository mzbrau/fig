using Fig.Datalayer.BusinessEntities;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class SettingsClientMap : ClassMapping<SettingClientBusinessEntity>
{
    public SettingsClientMap()
    {
        Table("setting_client");
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.Name, x => x.Column("name"));
        Property(x => x.Instance, x => x.Column("instance"));
        Property(x => x.ClientSecret, x => x.Column("client_secret"));
        Bag(x => x.Settings,
            x =>
            {
                x.Table("setting");
                x.Cascade(Cascade.All);
                x.Lazy(CollectionLazy.NoLazy);
            },
            x => x.OneToMany(a =>
            {
                a.Class(typeof(SettingBusinessEntity));
            }));

    }
}
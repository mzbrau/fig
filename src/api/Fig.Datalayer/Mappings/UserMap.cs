using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.Constants;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class UserMap : ClassMapping<UserBusinessEntity>
{
    public UserMap()
    {
        Table(Mapping.UsersTable);
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.Username, x => x.Column("username"));
        Property(x => x.FirstName, x => x.Column("first_name"));
        Property(x => x.LastName, x => x.Column("last_name"));
        Property(x => x.Role, x => x.Column("role"));
        Property(x => x.ClientFilter, x => x.Column("client_filter"));
        Property(x => x.AllowedClassificationsJson, x => x.Column("allowed_classifications_json"));
        Property(x => x.PasswordHash, x =>
        {
            x.Length(1000);
            x.Column("password_hash");
        });
    }
}
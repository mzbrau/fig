using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.Mappings;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Mapping.ByCode;
using NHibernate.Tool.hbm2ddl;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer;

public class FigSessionFactory : IFigSessionFactory
{
    private const string UserTableCreationPart = "create table users (";
    private readonly ILogger<FigSessionFactory> _logger;
    private Configuration? _configuration;
    private bool _isDatabaseNewlyCreated;
    private HbmMapping? _mapping;
    private ISessionFactory? _sessionFactory;

    public FigSessionFactory(ILogger<FigSessionFactory> logger)
    {
        _logger = logger;
        MigrateDatabase();
        CreateDefaultUser();
    }

    private ISessionFactory SessionFactory => _sessionFactory ??= Configuration.BuildSessionFactory();

    private Configuration Configuration => _configuration ??= CreateConfiguration();

    private HbmMapping Mapping => _mapping ??= CreateMapping();

    public ISession OpenSession()
    {
        //Open and return the nhibernate session
        return SessionFactory.OpenSession();
    }

    private void MigrateDatabase()
    {
        _logger.LogInformation("Performing database migration...");
        var schemaUpdate = new SchemaUpdate(Configuration);
        schemaUpdate.Execute(CheckForUserTableCreation, true);
        _logger.LogInformation("Database migration complete.");
    }

    private void CheckForUserTableCreation(string sql)
    {
        if (sql.Contains(UserTableCreationPart))
            _isDatabaseNewlyCreated = true;
    }

    private Configuration CreateConfiguration()
    {
        var configuration = new Configuration();

        //Loads properties from hibernate.cfg.xml
        configuration.Configure();

        //Loads nhibernate mappings 
        configuration.AddDeserializedMapping(Mapping, null);

        return configuration;
    }

    private HbmMapping CreateMapping()
    {
        var mapper = new ModelMapper();

        mapper.AddMappings(new List<Type>
        {
            typeof(SettingsClientMap),
            typeof(SettingMap),
            typeof(SettingValueMap),
            typeof(ClientStatusMap),
            typeof(ClientRunSessionMap),
            typeof(EventLogMap),
            typeof(SettingDynamicVerificationMap),
            typeof(SettingPluginVerificationMap),
            typeof(UserMap),
            typeof(CertificateMetadataMap),
            typeof(VerificationResultMap),
            typeof(FigConfigurationMapping)
        });

        return mapper.CompileMappingForAllExplicitlyAddedEntities();
    }

    private void CreateDefaultUser()
    {
        // Default user is only created when the database is being created.
        if (!_isDatabaseNewlyCreated)
            return;

        var defaultUser = new UserBusinessEntity
        {
            Username = "admin",
            FirstName = "Default",
            LastName = "User",
            Role = Role.Administrator,
            PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword("admin")
        };

        using var session = OpenSession();
        using var transaction = session.BeginTransaction();
        session.Save(defaultUser);
        transaction.Commit();
    }
}
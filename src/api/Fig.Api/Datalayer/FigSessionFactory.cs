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
    private readonly ILogger<FigSessionFactory> _logger;
    private Configuration? _configuration;
    private HbmMapping? _mapping;
    private ISessionFactory? _sessionFactory;

    public FigSessionFactory(ILogger<FigSessionFactory> logger)
    {
        _logger = logger;
        MigrateDatabase();
    }

    public ISession OpenSession()
    {
        //Open and return the nhibernate session
        return SessionFactory.OpenSession();
    }
    
    private ISessionFactory SessionFactory => _sessionFactory ??= Configuration.BuildSessionFactory();

    private Configuration Configuration => _configuration ??= CreateConfiguration();

    private HbmMapping Mapping => _mapping ??= CreateMapping();

    private void MigrateDatabase()
    {
        _logger.LogInformation("Performing database migration...");
        var schemaUpdate = new SchemaUpdate(Configuration);
        schemaUpdate.Execute(log => _logger.LogInformation(log), true);
        _logger.LogInformation("Database migration complete.");
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
            typeof(EventLogMap),
            typeof(SettingDynamicVerificationMap),
            typeof(SettingPluginVerificationMap),
            typeof(UserMap)
        });
        
        return mapper.CompileMappingForAllExplicitlyAddedEntities();
    }
}
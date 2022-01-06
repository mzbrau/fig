using Fig.Api.Datalayer.Mappings;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Mapping.ByCode;
using NHibernate.Tool.hbm2ddl;
using ISession = NHibernate.ISession;
using ISessionFactory = NHibernate.ISessionFactory;

namespace Fig.Api.Datalayer;

public class FigSessionFactory : IFigSessionFactory
{
    private readonly ILogger<FigSessionFactory> _logger;
    private ISessionFactory? _sessionFactory;
    private Configuration? _configuration;
    private HbmMapping? _mapping;

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

    private void MigrateDatabase()
    {
        _logger.LogInformation("Performing database migration...");
        var schemaUpdate = new SchemaUpdate(Configuration);
        schemaUpdate.Execute(log => _logger.LogInformation(log), true);
        _logger.LogInformation("Database migration complete.");
    }

    private ISessionFactory SessionFactory => _sessionFactory ??= Configuration.BuildSessionFactory();

    private Configuration Configuration => _configuration ??= CreateConfiguration();

    private HbmMapping Mapping => _mapping ??= CreateMapping();

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
        
        //Add the person mapping to the model mapper
        mapper.AddMappings(new List<Type>
        {
            typeof(SettingsClientMap),
            typeof(SettingMap),
            typeof(SettingValueMap)
        });
        
        //Create and return a HbmMapping of the model mapping in code
        return mapper.CompileMappingForAllExplicitlyAddedEntities();
    }
}
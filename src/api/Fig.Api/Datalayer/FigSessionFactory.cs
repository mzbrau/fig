using Fig.Api.Constants;
using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.Mappings;
using Microsoft.Extensions.Options;
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
    private readonly IOptions<ApiSettings> _settings;
    private Configuration? _configuration;
    private bool _isDatabaseNewlyCreated;
    private HbmMapping? _mapping;
    private ISessionFactory? _sessionFactory;

    public FigSessionFactory(ILogger<FigSessionFactory> logger, IOptions<ApiSettings> settings)
    {
        _logger = logger;
        _settings = settings;
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

        _logger.LogInformation(sql);
    }

    private Configuration CreateConfiguration()
    {
        var configuration = new Configuration();

        configuration.SetProperty("connection.connection_string", _settings.Value.DbConnectionString);
        configuration.SetProperty("connection.driver_class", GetDriverClass(_settings.Value.DbConnectionString));
        configuration.SetProperty("dialect", GetDialect(_settings.Value.DbConnectionString));

        //Loads properties from hibernate.cfg.xml
        configuration.Configure();

        //Loads nhibernate mappings 
        configuration.AddDeserializedMapping(Mapping, null);

        return configuration;
    }

    private string GetDialect(string connectionString)
    {
        return IsSqlLite(connectionString)
            ? "NHibernate.Dialect.SQLiteDialect"
            : "NHibernate.Dialect.MsSql2012Dialect";
    }

    private string GetDriverClass(string connectionString)
    {
        return IsSqlLite(connectionString)
            ? "NHibernate.Driver.SQLite20Driver"
            : "NHibernate.Driver.SqlClientDriver";
    }

    private bool IsSqlLite(string connectionString)
    {
        return connectionString.ToLower().Contains("data source");
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
            typeof(RunSessionMemoryUsageMap),
            typeof(EventLogMap),
            typeof(SettingDynamicVerificationMap),
            typeof(SettingPluginVerificationMap),
            typeof(UserMap),
            typeof(VerificationResultMap),
            typeof(LookupTableMap),
            typeof(FigConfigurationMapping),
            typeof(ApiStatusMap),
            typeof(DeferredClientImportMap),
            typeof(WebHookClientMap),
            typeof(WebHookMap)
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
            Username = DefaultUser.UserName,
            FirstName = "Default",
            LastName = "User",
            Role = Role.Administrator,
            PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(DefaultUser.Password)
        };

        using var session = OpenSession();
        using var transaction = session.BeginTransaction();
        session.Save(defaultUser);
        transaction.Commit();
    }
}
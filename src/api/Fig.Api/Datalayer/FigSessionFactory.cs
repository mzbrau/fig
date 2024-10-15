using System.Data.SqlClient;
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
        LogConnection();
        MigrateDatabase();
        CreateDefaultUser();
    }

    public ISessionFactory SessionFactory => _sessionFactory ??= Configuration.BuildSessionFactory();

    private Configuration Configuration => _configuration ??= CreateConfiguration();

    private HbmMapping Mapping => _mapping ??= CreateMapping();

    private void MigrateDatabase()
    {
        _logger.LogInformation("Starting database migration...");
    
        try
        {
            var schemaUpdate = new SchemaUpdate(Configuration);
        
            _logger.LogInformation("Checking for database connection...");
            schemaUpdate.Execute(CheckForUserTableCreation, true);
        
            _logger.LogInformation("Database migration completed successfully");
        }
        catch (HibernateException ex)
        {
            _logger.LogError(ex, "Failed to perform database migration: unable to connect to the database");
            throw; // Rethrow if you want the application to handle the failure further up.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during database migration");
            throw;
        }
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
            typeof(SettingVerificationMap),
            typeof(UserMap),
            typeof(VerificationResultMap),
            typeof(LookupTableMap),
            typeof(FigConfigurationMapping),
            typeof(ApiStatusMap),
            typeof(DeferredClientImportMap),
            typeof(WebHookClientMap),
            typeof(WebHookMap),
            typeof(SettingChangeMap)
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
            ClientFilter = ".*",
            PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(DefaultUser.Password)
        };

        using var session = SessionFactory.OpenSession();
        using var transaction = session.BeginTransaction();
        session.Save(defaultUser);
        transaction.Commit();
    }

    private void LogConnection()
    {
        if (string.IsNullOrWhiteSpace(_settings.Value.DbConnectionString))
            _logger.LogError("Connection string is null. Fig will not start.");

        if (!IsSqlLite(_settings.Value.DbConnectionString))
        {
            var builder = new SqlConnectionStringBuilder(_settings.Value.DbConnectionString);
            if (!string.IsNullOrWhiteSpace(builder.Password))
            {
                builder.Password = "******";
            }

            _logger.LogInformation("Connecting to database with connection string {ConnectionString}", builder.ToString());
        }
    }
}
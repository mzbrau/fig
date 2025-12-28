using System.Data.Common;
using Fig.Api.Constants;
using Fig.Api.ExtensionMethods;
using Fig.Client.Abstractions.Data;
using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.Mappings;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
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
    private readonly IConfiguration _appConfiguration;
    private Configuration? _configuration;
    private bool _isDatabaseNewlyCreated;
    private HbmMapping? _mapping;
    private ISessionFactory? _sessionFactory;
    private bool? _isSqlLite;

    public FigSessionFactory(ILogger<FigSessionFactory> logger, IOptions<ApiSettings> settings, IConfiguration appConfiguration)
    {
        _logger = logger;
        _settings = settings;
        _appConfiguration = appConfiguration;
        
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

            if (schemaUpdate.Exceptions.Any())
            {
                foreach (var exception in schemaUpdate.Exceptions)
                {
                    _logger.LogError(exception, "Exception while updating database schema: {Message}", exception.Message);
                }
            }
            else
            {
                _logger.LogInformation("Database migration completed successfully");
            }
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

        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        _logger.LogInformation(sql);
    }

    private Configuration CreateConfiguration()
    {
        var connectionString = PrepareConnectionString();
        var configuration = new Configuration();
        
        configuration.SetProperty("connection.connection_string", connectionString);
        configuration.SetProperty("connection.driver_class", GetDriverClass(connectionString));
        configuration.SetProperty("dialect", GetDialect(connectionString));

        //Loads properties from hibernate.cfg.xml
        configuration.Configure();

        //Loads nhibernate mappings 
        configuration.AddDeserializedMapping(Mapping, null);

        return configuration;
    }

    private string GetDialect(string? connectionString)
    {
        return IsSqlLite(connectionString)
            ? "NHibernate.Dialect.SQLiteDialect"
            : "NHibernate.Dialect.MsSql2012Dialect";
    }

    private string GetDriverClass(string? connectionString)
    {
        return IsSqlLite(connectionString)
            ? "NHibernate.Driver.SQLite20Driver"
            : "NHibernate.Driver.MicrosoftDataSqlClientDriver";
    }
    
    private bool IsSqlLite(string? connectionString)
    {
        if (_isSqlLite is not null)
            return _isSqlLite.Value;
        
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };

        // Check for SQLite indicators
        object? uri = null;
        if (builder.TryGetValue("Data Source", out var dataSource) || 
            builder.TryGetValue("URI", out uri))
        {
            string dataSourceValue = dataSource?.ToString() ?? uri?.ToString() ?? string.Empty;
            if (!string.IsNullOrEmpty(dataSourceValue) && 
                (dataSourceValue.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase) ||
                 dataSourceValue.EndsWith(".db", StringComparison.OrdinalIgnoreCase) ||
                 dataSourceValue.Equals(":memory:", StringComparison.OrdinalIgnoreCase) ||
                 dataSourceValue.StartsWith("file:", StringComparison.OrdinalIgnoreCase)))
            {
                _isSqlLite = true;
                return _isSqlLite.Value;
            }
        }

        _isSqlLite = false;
        return _isSqlLite.Value;
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
            typeof(UserMap),
            typeof(LookupTableMap),
            typeof(FigConfigurationMapping),
            typeof(ApiStatusMap),
            typeof(DeferredClientImportMap),
            typeof(WebHookClientMap),
            typeof(WebHookMap),
            typeof(SettingChangeMap),
            typeof(CheckPointMap),
            typeof(CheckPointDataMap),
            typeof(DeferredClientImportMap),
            typeof(DeferredChangeMap),
            typeof(CheckPointTriggerMap),
            typeof(CustomActionExecutionMap),
            typeof(CustomActionMap),
            typeof(DatabaseMigrationMap)
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
            PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(DefaultUser.Password),
            AllowedClassifications = Enum.GetValues(typeof(Classification)).Cast<Classification>().ToList()
        };

        using var session = SessionFactory.OpenSession();
        using var transaction = session.BeginTransaction();
        session.Save(defaultUser);
        transaction.Commit();
    }

    private string? PrepareConnectionString()
    {
        var connectionString = _appConfiguration.GetConnectionString("Fig");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = _settings.Value.DbConnectionString;
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogError("Connection string is null. Fig will not start");
            return null;
        }

        if (!IsSqlLite(connectionString))
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            
            // Log the connection string (with masked password)
            var logBuilder = new SqlConnectionStringBuilder(connectionString);
            if (!string.IsNullOrWhiteSpace(logBuilder.Password))
            {
                logBuilder.Password = "******";
            }
            _logger.LogInformation("Connecting to database with connection string {ConnectionString}", logBuilder.ToString());
            
            // Update connection string with MultipleActiveResultSets if needed
            if (!builder.MultipleActiveResultSets)
            {
                builder.MultipleActiveResultSets = true;
                connectionString = builder.ConnectionString;
            }
        }

        return connectionString.NormalizeToLegacyConnectionString();
    }
}
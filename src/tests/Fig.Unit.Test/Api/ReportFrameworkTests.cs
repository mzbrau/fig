using Fig.Api.Reports;
using Fig.Api.Reports.Implementations;
using Fig.Contracts.Reports;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class ReportParameterMetadataFactoryTests
{
    [Test]
    public void ShallGenerateMetadataFromParameterAttributes()
    {
        var metadata = ReportParameterMetadataFactory.Create(typeof(UserActivityParameters));

        Assert.That(metadata.Count, Is.EqualTo(3));
        Assert.That(metadata.Select(m => m.Name), Is.EquivalentTo(new[] { "Username", "From", "To" }));

        var username = metadata.Single(m => m.Name == "Username");
        Assert.That(username.DisplayName, Is.EqualTo("User"));
        Assert.That(username.Type, Is.EqualTo(ReportParameterType.String));
        Assert.That(username.Required, Is.True);
        Assert.That(username.LookupKind, Is.EqualTo(ReportParameterLookupKind.Users));

        var from = metadata.Single(m => m.Name == "From");
        Assert.That(from.Type, Is.EqualTo(ReportParameterType.DateTime));
        Assert.That(from.Required, Is.True);
    }

    [Test]
    public void ShallMarkNullableInstanceAsOptional()
    {
        var metadata = ReportParameterMetadataFactory.Create(typeof(ClientStatusParameters));
        var instance = metadata.Single(m => m.Name == "Instance");
        Assert.That(instance.Required, Is.False);
        Assert.That(instance.Type, Is.EqualTo(ReportParameterType.String));
    }

    [Test]
    public void ShallTreatBoolAndIntParametersAsOptionalWithDefaults()
    {
        var metadata = ReportParameterMetadataFactory.Create(typeof(ConfigurationInventoryParameters));
        var secretsOnly = metadata.Single(m => m.Name == "SecretsOnly");
        Assert.That(secretsOnly.Required, Is.False);
        Assert.That(secretsOnly.DefaultValue, Is.EqualTo(false));

        var fleet = ReportParameterMetadataFactory.Create(typeof(FleetHealthParameters));
        var minSessions = fleet.Single(m => m.Name == "MinSessions");
        Assert.That(minSessions.Required, Is.False);
        Assert.That(minSessions.DefaultValue, Is.EqualTo(1));
    }

    [Test]
    public void ShallExposeGroupsLookupForSettingGroupsCoverage()
    {
        var metadata = ReportParameterMetadataFactory.Create(typeof(SettingGroupsCoverageParameters));
        var groupName = metadata.Single(m => m.Name == "GroupName");
        Assert.That(groupName.LookupKind, Is.EqualTo(ReportParameterLookupKind.Groups));
        Assert.That(groupName.Required, Is.False);
    }

    [Test]
    public void ShallExposeClientAndSettingLookupsForBlastRadius()
    {
        var metadata = ReportParameterMetadataFactory.Create(typeof(BlastRadiusParameters));
        var client = metadata.Single(m => m.Name == "ClientName");
        var setting = metadata.Single(m => m.Name == "SettingName");

        Assert.That(client.LookupKind, Is.EqualTo(ReportParameterLookupKind.Clients));
        Assert.That(client.Required, Is.True);
        Assert.That(setting.LookupKind, Is.EqualTo(ReportParameterLookupKind.ClientSettings));
        Assert.That(setting.Required, Is.True);
    }
}

[TestFixture]
public class ReportParameterBinderTests
{
    private readonly ReportParameterBinder _binder = new();

    [Test]
    public void ShallBindStronglyTypedParameters()
    {
        var raw = new Dictionary<string, object?>
        {
            ["Username"] = "admin",
            ["From"] = DateTime.UtcNow.AddDays(-1).ToString("o"),
            ["To"] = DateTime.UtcNow.ToString("o")
        };

        var result = (UserActivityParameters)_binder.Bind(typeof(UserActivityParameters), raw);

        Assert.That(result.Username, Is.EqualTo("admin"));
        Assert.That(result.From.Kind, Is.EqualTo(DateTimeKind.Utc));
        Assert.That(result.To.Kind, Is.EqualTo(DateTimeKind.Utc));
    }

    [Test]
    public void ShallFailWhenRequiredParameterMissing()
    {
        var raw = new Dictionary<string, object?>
        {
            ["From"] = DateTime.UtcNow.AddDays(-1),
            ["To"] = DateTime.UtcNow
        };

        Assert.Throws<ReportParameterValidationException>(() =>
            _binder.Bind(typeof(UserActivityParameters), raw));
    }

    [Test]
    public void ShallBindCaseInsensitiveKeys()
    {
        var raw = new Dictionary<string, object?>
        {
            ["username"] = "admin",
            ["FROM"] = DateTime.UtcNow.AddDays(-1).ToString("o"),
            ["to"] = DateTime.UtcNow.ToString("o")
        };

        var result = (UserActivityParameters)_binder.Bind(typeof(UserActivityParameters), raw);
        Assert.That(result.Username, Is.EqualTo("admin"));
    }

    [Test]
    public void ShallKeepDefaultsWhenOptionalParametersMissing()
    {
        var result = (ConfigurationInventoryParameters)_binder.Bind(
            typeof(ConfigurationInventoryParameters),
            new Dictionary<string, object?>());

        Assert.That(result.SecretsOnly, Is.False);
        Assert.That(result.ClientName, Is.Null);
    }

    [Test]
    public void ShallFailWhenDateTimeInvalid()
    {
        var ex = Assert.Throws<ReportParameterValidationException>(() =>
            _binder.Bind(typeof(UserActivityParameters), new Dictionary<string, object?>
            {
                ["Username"] = "admin",
                ["From"] = "not-a-date",
                ["To"] = DateTime.UtcNow
            }));

        Assert.That(ex!.Message, Does.Contain("From"));
    }

    [Test]
    public void ShallFailWhenIntInvalid()
    {
        var ex = Assert.Throws<ReportParameterValidationException>(() =>
            _binder.Bind(typeof(FleetHealthParameters), new Dictionary<string, object?>
            {
                ["From"] = DateTime.UtcNow.AddDays(-1),
                ["To"] = DateTime.UtcNow,
                ["MinSessions"] = "abc"
            }));

        Assert.That(ex!.Message, Does.Contain("MinSessions"));
    }

    [Test]
    public void ShallBindJTokenValues()
    {
        var raw = new Dictionary<string, object?>
        {
            ["Username"] = new Newtonsoft.Json.Linq.JValue("admin"),
            ["From"] = new Newtonsoft.Json.Linq.JValue(DateTime.UtcNow.AddDays(-1).ToString("o")),
            ["To"] = new Newtonsoft.Json.Linq.JValue(DateTime.UtcNow.ToString("o"))
        };

        var result = (UserActivityParameters)_binder.Bind(typeof(UserActivityParameters), raw);
        Assert.That(result.Username, Is.EqualTo("admin"));
        Assert.That(result.From.Kind, Is.EqualTo(DateTimeKind.Utc));
    }
}

[TestFixture]
public class ReportRegistryTests
{
    [Test]
    public void ShallIndexReportsById()
    {
        var reports = new IReport[]
        {
            new StubReport("a", "Alpha", "Cat"),
            new StubReport("b", "Beta", "Cat")
        };

        var registry = new ReportRegistry(reports);

        Assert.That(registry.GetAll().Count, Is.EqualTo(2));
        Assert.That(registry.Get("a")?.Name, Is.EqualTo("Alpha"));
        Assert.That(registry.Get("missing"), Is.Null);
    }

    private sealed class StubReport : IReport
    {
        public StubReport(string id, string name, string category)
        {
            Id = id;
            Name = name;
            Category = category;
        }

        public string Id { get; }
        public string Name { get; }
        public string Category { get; }
        public string Description => "stub";
        public Type ParametersType => typeof(object);
        public Type BodyComponentType => typeof(object);
        public ReportPageOrientation PageOrientation => ReportPageOrientation.Portrait;
        public IList<ReportParameterDataContract> GetParameterDefinitions() => [];
        public Task<object> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
            => Task.FromResult<object>(new object());
    }
}

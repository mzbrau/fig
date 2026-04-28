using Fig.Api;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class ApiSettingsProxyTests
{
    private static readonly string[] ProxyEnvironmentVariables =
    [
        "HTTPS_PROXY",
        "https_proxy",
        "HTTP_PROXY",
        "http_proxy",
        "ALL_PROXY",
        "all_proxy"
    ];

    private Dictionary<string, string?> _originalValues = null!;

    [SetUp]
    public void SetUp()
    {
        _originalValues = ProxyEnvironmentVariables.ToDictionary(name => name, Environment.GetEnvironmentVariable);
        ClearProxyEnvironmentVariables();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var entry in _originalValues)
            Environment.SetEnvironmentVariable(entry.Key, entry.Value);
    }

    [Test]
    public void ShallReturnConfiguredOutboundProxyAddressBeforeEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("HTTPS_PROXY", "http://env-proxy:8080");
        var settings = new ApiSettings
        {
            DbConnectionString = "Data Source=fig.db;Version=3;New=True",
            OutboundHttpProxyAddress = "http://configured-proxy:3128"
        };

        var result = settings.GetOutboundHttpProxyAddress();

        Assert.That(result, Is.EqualTo("http://configured-proxy:3128"));
    }

    [Test]
    public void ShallReturnHttpsProxyEnvironmentVariableWhenNoConfiguredProxyExists()
    {
        Environment.SetEnvironmentVariable("HTTPS_PROXY", "http://env-proxy:8080");
        var settings = new ApiSettings
        {
            DbConnectionString = "Data Source=fig.db;Version=3;New=True"
        };

        var result = settings.GetOutboundHttpProxyAddress();

        Assert.That(result, Is.EqualTo("http://env-proxy:8080"));
    }

    [Test]
    public void ShallReturnNullWhenNoProxyConfigurationExists()
    {
        var settings = new ApiSettings
        {
            DbConnectionString = "Data Source=fig.db;Version=3;New=True"
        };

        var result = settings.GetOutboundHttpProxyAddress();

        Assert.That(result, Is.Null);
    }

    private static void ClearProxyEnvironmentVariables()
    {
        foreach (var variableName in ProxyEnvironmentVariables)
            Environment.SetEnvironmentVariable(variableName, null);
    }
}

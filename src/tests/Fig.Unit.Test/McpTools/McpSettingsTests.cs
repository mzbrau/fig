using Fig.Mcp.Configuration;
using NUnit.Framework;

namespace Fig.Unit.Test.McpTools;

[TestFixture]
public class McpSettingsTests
{
    [Test]
    public void Defaults_ShouldHaveExpectedValues()
    {
        var settings = new McpSettings();

        Assert.That(settings.FigApiBaseUrl, Is.EqualTo("https://localhost:7281"));
        Assert.That(settings.Username, Is.EqualTo("admin"));
        Assert.That(settings.Password, Is.Empty);
        Assert.That(settings.Transport, Is.EqualTo("stdio"));
    }

    [Test]
    public void Defaults_ToolGates_ShouldNotBeNull()
    {
        var settings = new McpSettings();

        Assert.That(settings.ToolGates, Is.Not.Null);
    }

    [Test]
    public void Properties_CanBeOverridden()
    {
        var settings = new McpSettings
        {
            FigApiBaseUrl = "https://fig.example.com",
            Username = "operator",
            Password = "secret",
            Transport = "sse"
        };

        Assert.That(settings.FigApiBaseUrl, Is.EqualTo("https://fig.example.com"));
        Assert.That(settings.Username, Is.EqualTo("operator"));
        Assert.That(settings.Password, Is.EqualTo("secret"));
        Assert.That(settings.Transport, Is.EqualTo("sse"));
    }
}

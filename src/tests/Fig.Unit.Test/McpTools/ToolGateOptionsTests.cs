using Fig.Mcp.Configuration;
using NUnit.Framework;

namespace Fig.Unit.Test.McpTools;

[TestFixture]
public class ToolGateOptionsTests
{
    [Test]
    public void Defaults_ReadOperations_ShouldBeEnabled()
    {
        var options = new ToolGateOptions();

        Assert.That(options.ReadSettings, Is.True);
        Assert.That(options.ReadEvents, Is.True);
        Assert.That(options.ReadSessions, Is.True);
        Assert.That(options.ReadHistory, Is.True);
    }

    [Test]
    public void Defaults_WriteOperations_ShouldBeDisabled()
    {
        var options = new ToolGateOptions();

        Assert.That(options.WriteSettings, Is.False);
        Assert.That(options.ManageClients, Is.False);
        Assert.That(options.DeleteClients, Is.False);
        Assert.That(options.ManageUsers, Is.False);
        Assert.That(options.ManageWebHooks, Is.False);
        Assert.That(options.ManageLookupTables, Is.False);
        Assert.That(options.ManageScheduling, Is.False);
        Assert.That(options.ManageTimeMachine, Is.False);
        Assert.That(options.ExecuteCustomActions, Is.False);
        Assert.That(options.ImportExportData, Is.False);
        Assert.That(options.ManageConfiguration, Is.False);
    }

    [Test]
    public void WriteOperations_CanBeExplicitlyEnabled()
    {
        var options = new ToolGateOptions
        {
            WriteSettings = true,
            ManageClients = true,
            DeleteClients = true
        };

        Assert.That(options.WriteSettings, Is.True);
        Assert.That(options.ManageClients, Is.True);
        Assert.That(options.DeleteClients, Is.True);
    }

    [Test]
    public void ReadOperations_CanBeExplicitlyDisabled()
    {
        var options = new ToolGateOptions
        {
            ReadSettings = false,
            ReadEvents = false,
            ReadSessions = false,
            ReadHistory = false
        };

        Assert.That(options.ReadSettings, Is.False);
        Assert.That(options.ReadEvents, Is.False);
        Assert.That(options.ReadSessions, Is.False);
        Assert.That(options.ReadHistory, Is.False);
    }
}

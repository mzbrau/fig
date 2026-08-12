using Fig.Web.Dashboards;
using Fig.Web.Dashboards.Runtime;
using NUnit.Framework;

namespace Fig.Unit.Test.Web.Dashboards;

[TestFixture]
public class DashboardDataExplorerModelTests
{
    [Test]
    public void Build_UsesExactJintPropertyNames()
    {
        var fig = new DashboardFigRoot
        {
            clients = new DashboardJsArray(new object?[]
            {
                new DashboardClientJsModel
                {
                    name = "Api",
                    settings = new Dictionary<string, object?> { ["Port"] = 8080 }
                }
            }),
            runSessions = new DashboardJsArray(new object?[]
            {
                new DashboardRunSessionJsModel
                {
                    name = "Api",
                    hostname = "host-a",
                    health = new DashboardHealthJsModel { status = "Healthy" },
                    customProperties = new Dictionary<string, object?> { ["Region"] = "eu" }
                }
            })
        };

        var transforms = new Dictionary<string, object?>
        {
            ["byName"] = new[] { new { label = "Api", value = 1 } }
        };

        var roots = DashboardDataExplorerModel.Build(fig, transforms);
        Assert.That(roots.Select(r => r.Name), Is.EqualTo(new[] { "fig", "transforms" }));

        var figNode = roots[0];
        Assert.That(figNode.Children.Select(c => c.Name), Is.EqualTo(new[] { "clients", "runSessions" }));

        var clients = figNode.Children.First(c => c.Name == "clients");
        Assert.That(clients.Children, Has.Count.EqualTo(1));
        Assert.That(clients.Children[0].Name, Is.EqualTo("[0]"));
        var settings = clients.Children[0].Children.First(c => c.Name == "settings");
        Assert.That(settings.Children[0].Name, Is.EqualTo("Port"));
        Assert.That(settings.Children[0].Value, Is.EqualTo("8080"));

        var sessions = figNode.Children.First(c => c.Name == "runSessions");
        Assert.That(sessions.Children[0].Name, Is.EqualTo("[0]"));
        Assert.That(sessions.Children[0].Children.Any(c => c.Name == "hostname"), Is.True);
        Assert.That(sessions.Children[0].Children.First(c => c.Name == "hostname").Value, Is.EqualTo("host-a"));

        var custom = sessions.Children[0].Children.First(c => c.Name == "customProperties");
        Assert.That(custom.HasChildren, Is.True);
        Assert.That(custom.Children[0].Name, Is.EqualTo("Region"));
        Assert.That(custom.Children[0].Value, Is.EqualTo("eu"));

        Assert.That(roots[1].Children[0].Name, Is.EqualTo("byName"));
    }

    [Test]
    public void FormatValue_SerializesObjectsWithNewtonsoft()
    {
        var json = DashboardDataExplorerModel.FormatValue(new { label = "x", value = 2 });
        Assert.That(json, Does.Contain("\"label\": \"x\""));
        Assert.That(json, Does.Contain("\"value\": 2"));
    }
}

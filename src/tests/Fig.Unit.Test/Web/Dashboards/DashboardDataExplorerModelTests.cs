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

        var roots = DashboardDataExplorerModel.Build(fig);
        Assert.That(roots.Select(r => r.Name), Is.EqualTo(new[] { "fig" }));
        Assert.That(roots[0].Path, Is.EqualTo("fig"));

        var figNode = roots[0];
        Assert.That(figNode.Children.Select(c => c.Name), Is.EqualTo(new[] { "clients", "runSessions" }));
        Assert.That(figNode.Children.Select(c => c.Path), Is.EqualTo(new[] { "fig.clients", "fig.runSessions" }));

        var clients = figNode.Children.First(c => c.Name == "clients");
        Assert.That(clients.Children, Has.Count.EqualTo(1));
        Assert.That(clients.Children[0].Name, Is.EqualTo("[0]"));
        Assert.That(clients.Children[0].Path, Is.EqualTo("fig.clients[0]"));
        var settings = clients.Children[0].Children.First(c => c.Name == "settings");
        Assert.That(settings.Path, Is.EqualTo("fig.clients[0].settings"));
        Assert.That(settings.Children[0].Name, Is.EqualTo("Port"));
        Assert.That(settings.Children[0].Path, Is.EqualTo("fig.clients[0].settings.Port"));
        Assert.That(settings.Children[0].Value, Is.EqualTo("8080"));

        var sessions = figNode.Children.First(c => c.Name == "runSessions");
        Assert.That(sessions.Children[0].Name, Is.EqualTo("[0]"));
        Assert.That(sessions.Children[0].Path, Is.EqualTo("fig.runSessions[0]"));
        var hostname = sessions.Children[0].Children.First(c => c.Name == "hostname");
        Assert.That(hostname.Path, Is.EqualTo("fig.runSessions[0].hostname"));
        Assert.That(hostname.Value, Is.EqualTo("host-a"));

        var custom = sessions.Children[0].Children.First(c => c.Name == "customProperties");
        Assert.That(custom.HasChildren, Is.True);
        Assert.That(custom.Children[0].Name, Is.EqualTo("Region"));
        Assert.That(custom.Children[0].Path, Is.EqualTo("fig.runSessions[0].customProperties.Region"));
        Assert.That(custom.Children[0].Value, Is.EqualTo("eu"));
    }

    [Test]
    public void Build_UsesBracketNotationForNonIdentifierDictKeys()
    {
        var fig = new DashboardFigRoot
        {
            clients = new DashboardJsArray(new object?[]
            {
                new DashboardClientJsModel
                {
                    name = "Api",
                    settings = new Dictionary<string, object?> { ["my-key"] = "v", ["with space"] = 1 }
                }
            }),
            runSessions = new DashboardJsArray(Array.Empty<object?>())
        };

        var roots = DashboardDataExplorerModel.Build(fig);
        var settings = roots[0].Children
            .First(c => c.Name == "clients").Children[0]
            .Children.First(c => c.Name == "settings");

        var hyphen = settings.Children.First(c => c.Name == "my-key");
        Assert.That(hyphen.Path, Is.EqualTo("fig.clients[0].settings[\"my-key\"]"));

        var spaced = settings.Children.First(c => c.Name == "with space");
        Assert.That(spaced.Path, Is.EqualTo("fig.clients[0].settings[\"with space\"]"));
    }

    [Test]
    public void FormatPathSegment_UsesDotSafeOrBracketForm()
    {
        Assert.That(DashboardDataExplorerModel.FormatPathSegment("Port"), Is.EqualTo("Port"));
        Assert.That(DashboardDataExplorerModel.FormatPathSegment("_private"), Is.EqualTo("_private"));
        Assert.That(DashboardDataExplorerModel.FormatPathSegment("my-key"), Is.EqualTo("[\"my-key\"]"));
        Assert.That(DashboardDataExplorerModel.JoinPath("fig.clients[0].settings", "Port"),
            Is.EqualTo("fig.clients[0].settings.Port"));
        Assert.That(DashboardDataExplorerModel.JoinPath("fig.clients[0].settings", "[\"my-key\"]"),
            Is.EqualTo("fig.clients[0].settings[\"my-key\"]"));
        Assert.That(DashboardDataExplorerModel.JoinPath("fig.runSessions", "[0]"),
            Is.EqualTo("fig.runSessions[0]"));
    }

    [Test]
    public void FormatValue_SerializesObjectsWithNewtonsoft()
    {
        var json = DashboardDataExplorerModel.FormatValue(new { label = "x", value = 2 });
        Assert.That(json, Does.Contain("\"label\": \"x\""));
        Assert.That(json, Does.Contain("\"value\": 2"));
    }
}

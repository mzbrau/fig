using Fig.Common.NetStandard.Scripting;
using Fig.Contracts.Dashboards;
using Fig.Web.Dashboards.Runtime;
using Fig.Web.Scripting;
using NUnit.Framework;

namespace Fig.Unit.Test.Web.Dashboards;

[TestFixture]
public class DashboardDependencyResolverTests
{
    private readonly DashboardDependencyResolver _resolver = new();

    [Test]
    public void ShallOrderDependenciesTopologically()
    {
        var order = _resolver.ResolveOrder(new[]
        {
            ("c", (IReadOnlyList<string>)new[] { "b" }),
            ("a", (IReadOnlyList<string>)Array.Empty<string>()),
            ("b", (IReadOnlyList<string>)new[] { "a" })
        });

        Assert.That(order, Is.EqualTo(new[] { "a", "b", "c" }));
    }

    [Test]
    public void ShallDetectCycles()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => _resolver.ResolveOrder(new[]
        {
            ("a", (IReadOnlyList<string>)new[] { "b" }),
            ("b", (IReadOnlyList<string>)new[] { "a" })
        }));

        Assert.That(ex!.Message, Does.Contain("Circular"));
    }

    [Test]
    public void ShallRejectUnknownDependency()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => _resolver.ResolveOrder(new[]
        {
            ("a", (IReadOnlyList<string>)new[] { "missing" })
        }));

        Assert.That(ex!.Message, Does.Contain("unknown transform"));
    }
}

[TestFixture]
public class DashboardTransformEngineTests
{
    private readonly DashboardTransformEngine _engine = new(new JintEngineFactory());

    private static DashboardFigRoot SampleFig()
    {
        return new DashboardFigRoot
        {
            runSessions = new DashboardJsArray(new object?[]
            {
                new DashboardRunSessionJsModel { name = "Orders", applicationVersion = "1.0.0" },
                new DashboardRunSessionJsModel { name = "Orders", applicationVersion = "1.1.0" },
                new DashboardRunSessionJsModel { name = "Billing", applicationVersion = "1.0.0" }
            })
        };
    }

    [Test]
    public void ShallCountRunSessions()
    {
        var result = _engine.ExecuteScript("return fig.runSessions.length;", SampleFig());
        Assert.That(Convert.ToInt32(result), Is.EqualTo(3));
    }

    [Test]
    public void ShallGroupByApplicationVersion()
    {
        var result = _engine.ExecuteScript(
            """
            return fig.runSessions
                .groupBy(s => s.applicationVersion)
                .map(g => ({ label: g.key, value: g.items.length }));
            """,
            SampleFig());

        Assert.That(result, Is.InstanceOf<DashboardJsArray>());
        var array = (DashboardJsArray)result!;
        Assert.That(array.length, Is.EqualTo(2));
    }

    [Test]
    public void ShallUseNamedTransformResults()
    {
        var named = new Dictionary<string, object?>
        {
            ["sessionCount"] = 3
        };

        var result = _engine.ExecuteScript("return transforms.sessionCount;", SampleFig(), named);
        Assert.That(Convert.ToInt32(result), Is.EqualTo(3));
    }

    [Test]
    public void ShallIsolateInvalidScriptAsException()
    {
        Assert.Throws<Jint.Runtime.JavaScriptException>(
            () => _engine.ExecuteScript("return totally.broken();", SampleFig()));
    }
}

[TestFixture]
public class DashboardJsArrayTests
{
    [Test]
    public void ShallFilterMapAndAggregate()
    {
        var array = new DashboardJsArray(new object?[] { 1, 2, 3, 4 });
        var filtered = array.filter(x => Convert.ToInt32(x) % 2 == 0);
        Assert.That(filtered.length, Is.EqualTo(2));
        Assert.That(filtered.sum(), Is.EqualTo(6));
        Assert.That(filtered.average(), Is.EqualTo(3));
        Assert.That(Convert.ToInt32(filtered.first()), Is.EqualTo(2));
        Assert.That(Convert.ToInt32(filtered.last()), Is.EqualTo(4));
    }
}

[TestFixture]
public class DashboardDefinitionNormalizeTests
{
    [Test]
    public void PrepareScript_WrapsReturnStatements()
    {
        var prepared = DashboardTransformEngine.PrepareScript("return 1;");
        Assert.That(prepared, Does.StartWith("(function(){"));
        Assert.That(prepared, Does.EndWith("})()"));
    }
}

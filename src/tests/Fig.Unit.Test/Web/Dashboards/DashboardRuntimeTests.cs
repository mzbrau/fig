using Fig.Common.NetStandard.Scripting;
using Fig.Contracts.Dashboards;
using Fig.Web.Dashboards.Components;
using Fig.Web.Dashboards.Runtime;
using Fig.Web.Scripting;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Fig.Unit.Test.Web.Dashboards;

[TestFixture]
public class DashboardTransformEngineTests
{
    private readonly DashboardTransformEngine _engine = DashboardTestHelpers.CreateTransformEngine();

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
    public void ShallInvokeCountMethodViaJint()
    {
        var result = _engine.ExecuteScript(
            """
            return fig.runSessions.count(s => s.name === 'Orders');
            """,
            SampleFig());

        Assert.That(Convert.ToInt32(result), Is.EqualTo(2));
    }

    [Test]
    public void ShallIsolateInvalidScriptAsException()
    {
        Assert.Throws<Jint.Runtime.JavaScriptException>(
            () => _engine.ExecuteScript("return totally.broken();", SampleFig()));
    }

    [Test]
    public void ShallRefuseScriptExecutionWhenJavascriptDisabled()
    {
        var engine = DashboardTestHelpers.CreateTransformEngine(allowDisplayScripts: false);

        Assert.That(
            () => engine.ExecuteScript("return 1;", SampleFig()),
            Throws.InvalidOperationException.With.Message.Contains("disabled"));
    }
}

[TestFixture]
public class DashboardRuntimeEvaluateTests
{
    private sealed class FixedDataProvider : IDashboardDataProvider
    {
        public FixedDataProvider(DashboardFigRoot current) => Current = current;

        public DashboardFigRoot Current { get; }

        public DateTime? SettingsLastRefreshUtc => null;

        public DateTime? StatusLastRefreshUtc => null;

        public Task EnsureLoadedAsync() => Task.CompletedTask;

        public Task RefreshAllAsync() => Task.CompletedTask;

        public Task RefreshSettingsAsync() => Task.CompletedTask;

        public Task RefreshStatusAsync() => Task.CompletedTask;
    }

    [Test]
    public void Evaluate_UsesInlineScript()
    {
        var fig = new DashboardFigRoot
        {
            runSessions = new DashboardJsArray(new object?[]
            {
                new DashboardRunSessionJsModel { name = "Orders" }
            })
        };

        var runtime = new DashboardRuntime(DashboardTestHelpers.CreateTransformEngine(), new FixedDataProvider(fig));
        runtime.SetDefinition(new DashboardDefinitionDataContract
        {
            Components =
            [
                new DashboardComponentInstanceDataContract
                {
                    Id = "kpi-1",
                    Type = "kpi",
                    DataBinding = new DashboardDataBindingDataContract
                    {
                        InlineScript = "return { value: fig.runSessions.length, label: 'Sessions' };"
                    }
                }
            ]
        });

        var results = runtime.Evaluate();
        Assert.That(results["kpi-1"].Success, Is.True);
        Assert.That(results["kpi-1"].Data, Is.Not.Null);
    }

    [Test]
    public void Evaluate_FailsWhenInlineScriptMissing()
    {
        var runtime = new DashboardRuntime(
            DashboardTestHelpers.CreateTransformEngine(),
            new FixedDataProvider(new DashboardFigRoot()));
        runtime.SetDefinition(new DashboardDefinitionDataContract
        {
            Components =
            [
                new DashboardComponentInstanceDataContract
                {
                    Id = "kpi-1",
                    Type = "kpi",
                    DataBinding = new DashboardDataBindingDataContract()
                }
            ]
        });

        var results = runtime.Evaluate();
        Assert.That(results["kpi-1"].Success, Is.False);
        Assert.That(results["kpi-1"].Error, Does.Contain("InlineScript"));
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
    public void PrepareScript_WrapsTopLevelReturnStatements()
    {
        var prepared = DashboardTransformEngine.PrepareScript("return 1;");
        Assert.That(prepared, Does.StartWith("(function(){"));
        Assert.That(prepared, Does.EndWith("})()"));
    }

    [Test]
    public void PrepareScript_DoesNotWrapExpressionContainingReturnInString()
    {
        const string script = "({ text: 'return to normal' })";
        var prepared = DashboardTransformEngine.PrepareScript(script);
        Assert.That(prepared, Is.EqualTo(script));
    }

    [Test]
    public void PrepareScript_DoesNotTreatNestedFunctionReturnAsTopLevel()
    {
        const string script = "({ value: (function(){ return 1 })() })";
        var prepared = DashboardTransformEngine.PrepareScript(script);
        Assert.That(prepared, Is.EqualTo(script));
        Assert.That(DashboardTransformEngine.ContainsTopLevelReturn(script), Is.False);
    }

    [Test]
    public void PrepareScript_IgnoresReturnInComments()
    {
        const string script = "// return early\n({ value: 1 })";
        Assert.That(DashboardTransformEngine.ContainsTopLevelReturn(script), Is.False);
        Assert.That(DashboardTransformEngine.PrepareScript(script), Is.EqualTo(script.Trim()));
    }
}

[TestFixture]
public class DashboardSuggestedScriptTests
{
    [Test]
    public void ApplySuggestedScript_SetsInlineScriptAndConfig()
    {
        var registry = new DashboardComponentRegistry();
        var preset = registry.GetPreset("count-run-sessions");
        Assert.That(preset, Is.Not.Null);

        var component = new DashboardComponentInstanceDataContract
        {
            Id = "kpi-1",
            Type = "kpi",
            Config = new JObject { ["title"] = "Old" },
            DataBinding = new DashboardDataBindingDataContract { InlineScript = "return null;" }
        };

        DashboardComponentPropertiesForm.ApplySuggestedScript(component, preset!);

        Assert.That(component.DataBinding.InlineScript, Is.EqualTo(preset!.Script));
        Assert.That(component.Config?["title"]?.ToString(), Is.EqualTo("Connected run sessions"));
    }

    [Test]
    public void Registry_HasStarterPresetsForEveryComponentType()
    {
        var registry = new DashboardComponentRegistry();
        foreach (var descriptor in registry.All)
        {
            Assert.That(
                registry.PresetsFor(descriptor.Type).Any(),
                Is.True,
                $"Expected at least one suggested script for component type '{descriptor.Type}'.");
        }
    }
}

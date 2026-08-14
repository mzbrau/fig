using Fig.Web.Dashboards.Components;
using Fig.Web.Dashboards.Runtime;
using Fig.Web.Dashboards.Scripting;
using NUnit.Framework;

namespace Fig.Unit.Test.Web.Dashboards;

[TestFixture]
public class DashboardScriptAiPromptBuilderTests
{
    [Test]
    public void Build_IncludesContextForCardsComponent()
    {
        const string currentScript = "return fig.clients.map(c => ({ title: c.name, value: '1' }));";
        var registry = new DashboardComponentRegistry();
        var descriptor = registry.Get("cards");
        Assert.That(descriptor, Is.Not.Null);

        var prompt = DashboardScriptAiPromptBuilder.Build(
            "cards",
            descriptor!.DisplayName,
            descriptor.ExpectedScriptShape,
            currentScript);

        Assert.That(prompt, Does.Contain("USER REQUEST: <describe what you want this visualization to show>"));
        Assert.That(prompt, Does.Contain("Type: cards"));
        Assert.That(prompt, Does.Contain(descriptor.ExpectedScriptShape));
        Assert.That(prompt, Does.Contain("ExpectedScriptResult"));
        Assert.That(prompt, Does.Contain("fig.runSessions"));
        Assert.That(prompt, Does.Contain("fig.clients"));
        Assert.That(prompt, Does.Contain("Object.keys"));
        Assert.That(prompt, Does.Contain("Array.isArray"));
        Assert.That(prompt, Does.Contain("filter"));
        Assert.That(prompt, Does.Contain("groupBy"));
        Assert.That(prompt, Does.Contain("## Current script"));
        Assert.That(prompt, Does.Contain(currentScript));
        Assert.That(prompt, Does.Contain("```javascript"));
        Assert.That(prompt, Does.Contain("markdown code block"));
        Assert.That(prompt, Does.Not.Contain("no markdown fences"));

        // Full ambient model — property names and types for the AI
        Assert.That(prompt, Does.Contain("interface DashboardClient"));
        Assert.That(prompt, Does.Contain("interface DashboardRunSession"));
        Assert.That(prompt, Does.Contain("settings"));
        Assert.That(prompt, Does.Contain("uptimePercent24Hr"));
        Assert.That(prompt, Does.Contain("memoryUsageBytes"));
        Assert.That(prompt, Does.Contain("customProperties"));
        Assert.That(prompt, Does.Not.Contain("## Live keys (from current data)"));
    }

    [Test]
    public void Build_IncludesLiveKeysWhenFigProvided()
    {
        var fig = new DashboardFigRoot
        {
            clients = new DashboardJsArray(
            [
                new DashboardClientJsModel
                {
                    name = "Api",
                    settings = new Dictionary<string, object?>
                    {
                        ["ApiBaseUrl"] = "https://example",
                        ["MaxRetries"] = 3
                    }
                }
            ]),
            runSessions = new DashboardJsArray(
            [
                new DashboardRunSessionJsModel
                {
                    name = "Api",
                    customProperties = new Dictionary<string, object?>
                    {
                        ["isMaster"] = true,
                        ["lastSyncUtc"] = "2026-01-01T00:00:00Z"
                    }
                }
            ])
        };

        var prompt = DashboardScriptAiPromptBuilder.Build(
            "kpi",
            "KPI",
            "object { value }",
            null,
            fig);

        Assert.That(prompt, Does.Contain("## Live keys (from current data)"));
        Assert.That(prompt, Does.Contain("LiveSettingKey"));
        Assert.That(prompt, Does.Contain("ApiBaseUrl"));
        Assert.That(prompt, Does.Contain("MaxRetries"));
        Assert.That(prompt, Does.Contain("LiveCustomPropertyKey"));
        Assert.That(prompt, Does.Contain("isMaster"));
        Assert.That(prompt, Does.Contain("lastSyncUtc"));
    }

    [Test]
    public void Build_OmitsCurrentScriptSectionWhenEmpty()
    {
        var prompt = DashboardScriptAiPromptBuilder.Build(
            "kpi",
            "KPI",
            "object { value }",
            "   ");

        Assert.That(prompt, Does.Not.Contain("## Current script"));
        Assert.That(prompt, Does.Contain("Type: kpi"));
        Assert.That(prompt, Does.Contain("ExpectedScriptResult"));
    }
}

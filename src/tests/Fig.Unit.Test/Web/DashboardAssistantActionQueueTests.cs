using Fig.Web.Dashboards.Components;
using Fig.Web.Services.Assistant;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class DashboardAssistantActionQueueTests
{
    [Test]
    public void EnqueueInlineScriptUpdate_DequeuesMatchingDashboardInOrder()
    {
        var queue = new DashboardAssistantActionQueue();
        var raised = 0;
        queue.ActionsQueued += () => raised++;

        var dashboardA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var dashboardB = Guid.Parse("22222222-2222-2222-2222-222222222222");

        queue.EnqueueInlineScriptUpdate("kpi-1", "return 1;", dashboardA);
        queue.EnqueueInlineScriptUpdate("text-2", "return { text: 'hi' };", dashboardB);
        queue.EnqueueInlineScriptUpdate("badge-3", "return 'ok';");

        Assert.That(raised, Is.EqualTo(3));

        var forA = queue.DequeueForDashboard(dashboardA);
        Assert.That(forA, Has.Count.EqualTo(2));
        Assert.That(forA[0].ComponentId, Is.EqualTo("kpi-1"));
        Assert.That(forA[0].DashboardId, Is.EqualTo(dashboardA));
        Assert.That(forA[1].ComponentId, Is.EqualTo("badge-3"));
        Assert.That(forA[1].DashboardId, Is.Null);

        var forB = queue.DequeueForDashboard(dashboardB);
        Assert.That(forB, Has.Count.EqualTo(1));
        Assert.That(forB[0].ComponentId, Is.EqualTo("text-2"));
        Assert.That(queue.DequeueForDashboard(dashboardA), Is.Empty);
    }
}

[TestFixture]
public class DashboardComponentRegistryAssistantShapeTests
{
    [Test]
    public void AllDescriptors_HaveIconAndExpectedScriptShape()
    {
        var registry = new DashboardComponentRegistry();
        Assert.That(registry.All, Is.Not.Empty);
        foreach (var descriptor in registry.All)
        {
            Assert.That(descriptor.Icon, Is.Not.Null.And.Not.Empty, descriptor.Type);
            Assert.That(descriptor.ExpectedScriptShape, Is.Not.Null.And.Not.Empty, descriptor.Type);
        }

        Assert.That(DashboardComponentRegistry.JsModelSummary, Does.Contain("fig.runSessions"));
        Assert.That(registry.Get("kpi")!.Icon, Is.EqualTo("trending_up"));
        Assert.That(registry.Get("donut")!.Icon, Is.EqualTo("donut_large"));
        Assert.That(registry.Get("cards")!.Icon, Is.EqualTo("dashboard"));
        Assert.That(registry.GetPreset("replica-count-status"), Is.Not.Null);
        Assert.That(registry.GetPreset("master-and-last-sync"), Is.Not.Null);
        Assert.That(registry.GetPreset("all-clients-overview"), Is.Not.Null);
    }
}

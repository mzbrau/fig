using Fig.Web.Dashboards.Components;
using Fig.Web.Services.Assistant;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class DashboardAssistantActionQueueTests
{
    [Test]
    public void EnqueueInlineScriptUpdate_DequeuesInOrder()
    {
        var queue = new DashboardAssistantActionQueue();
        var raised = 0;
        queue.ActionsQueued += () => raised++;

        queue.EnqueueInlineScriptUpdate("kpi-1", "return 1;");
        queue.EnqueueInlineScriptUpdate("text-2", "return { text: 'hi' };");

        Assert.That(raised, Is.EqualTo(2));
        var actions = queue.DequeueAll();
        Assert.That(actions, Has.Count.EqualTo(2));
        Assert.That(actions[0].ComponentId, Is.EqualTo("kpi-1"));
        Assert.That(actions[1].ComponentId, Is.EqualTo("text-2"));
        Assert.That(queue.DequeueAll(), Is.Empty);
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
    }
}

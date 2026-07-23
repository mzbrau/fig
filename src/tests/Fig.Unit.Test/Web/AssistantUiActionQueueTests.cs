using Fig.Web.Services.Assistant;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class AssistantUiActionQueueTests
{
    [Test]
    public void EnqueueSearchAndHighlight_AreDequeuedInOrder()
    {
        var queue = new AssistantUiActionQueue();
        var raised = 0;
        queue.ActionsQueued += () => raised++;

        queue.EnqueueSearch("client:A");
        queue.EnqueueHighlight("A", "Items", null);

        Assert.That(raised, Is.EqualTo(2));

        var actions = queue.DequeueAll();
        Assert.That(actions, Has.Count.EqualTo(2));
        Assert.That(actions[0].Kind, Is.EqualTo(AssistantUiActionKind.Search));
        Assert.That(actions[0].SearchQuery, Is.EqualTo("client:A"));
        Assert.That(actions[1].Kind, Is.EqualTo(AssistantUiActionKind.Highlight));
        Assert.That(actions[1].ClientName, Is.EqualTo("A"));
        Assert.That(actions[1].SettingName, Is.EqualTo("Items"));
        Assert.That(queue.DequeueAll(), Is.Empty);
    }

    [Test]
    public void EnqueueSearch_WhenEmpty_Throws()
    {
        var queue = new AssistantUiActionQueue();
        Assert.That(() => queue.EnqueueSearch("  "), Throws.ArgumentException);
    }
}

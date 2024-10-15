using Fig.Common.Events;
using Fig.Web.Events;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

public class EventDistributorTests
{
    [Test]
    public void ShallCallbackSubscriber()
    {
        var distributor = new EventDistributor(Mock.Of<ILogger<EventDistributor>>());
        int count = 0;
        distributor.Subscribe("test", () => count++);
        
        distributor.Publish("test");
        
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public void ShallCallbackMultipleSubscribers()
    {
        var distributor = new EventDistributor(Mock.Of<ILogger<EventDistributor>>());
        int count1 = 0;
        int count2 = 0;
        distributor.Subscribe("test", () => count1++);
        distributor.Subscribe("test", () => count2++);
        
        distributor.Publish("test");
        
        Assert.That(count1, Is.EqualTo(1));
        Assert.That(count2, Is.EqualTo(1));
    }
    
    [Test]
    public void ShallNotCallbackOtherTopics()
    {
        var distributor = new EventDistributor(Mock.Of<ILogger<EventDistributor>>());
        int count1 = 0;
        int count2 = 0;
        distributor.Subscribe("test", () => count1++);
        distributor.Subscribe("other", () => count2++);
        
        distributor.Publish("test");
        
        Assert.That(count1, Is.EqualTo(1));
        Assert.That(count2, Is.EqualTo(0));
    }
}
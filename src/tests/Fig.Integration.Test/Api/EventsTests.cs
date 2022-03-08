using System;
using System.Linq;
using System.Threading.Tasks;
using Fig.Integration.Test.Api.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class EventsTests : IntegrationTestBase
{
    [SetUp]
    public async Task Setup()
    {
        await DeleteAllClients();
    }

    [TearDown]
    public async Task TearDown()
    {
        await DeleteAllClients();
    }

    [Test]
    public async Task ShallLogRegistrationEvents()
    {
        var startTime = DateTime.UtcNow;
        var settings = await RegisterSettings<ThreeSettings>();
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);
        
        Assert.That(result.Events.Count(), Is.EqualTo(1));
        var firstEvent = result.Events.First();
        Assert.That(firstEvent.EventType, Is.EqualTo("Initial Registration"));
        Assert.That(firstEvent.ClientName, Is.EqualTo(settings.ClientName));
    }
    
    [Test]
    public async Task ShallOnlyReturnEventsWithinTheTimeRange()
    {
        await RegisterSettings<ClientA>();
        var startTime = DateTime.UtcNow;
        var settings = await RegisterSettings<ThreeSettings>();
        var endTime = DateTime.UtcNow;
        var result = await GetEvents(startTime, endTime);
        
        Assert.That(result.Events.Count(), Is.EqualTo(1));
        var firstEvent = result.Events.First();
        Assert.That(firstEvent.ClientName, Is.EqualTo(settings.ClientName));
    }
}
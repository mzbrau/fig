using System;
using System.Linq;
using System.Threading.Tasks;
using Fig.Test.Common;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public class ApiStatusTests : IntegrationTestBase
{
    [Test]
    [Retry(3)]
    public async Task ShallGetApiStatus()
    {
        // Wait for the ApiStatusMonitor background service to register at least one API status
        await WaitForCondition(
            async () => (await GetAllApiStatuses()).Count > 0,
            TimeSpan.FromSeconds(10),
            () => "ApiStatusMonitor should have registered at least one API status");
        
        var statuses = await GetAllApiStatuses();
        
        Assert.That(statuses.Count, Is.AtLeast(1));
        Assert.That(statuses.All(a => a.Hostname == Environment.MachineName), "Status should be from this api");
        Assert.That(statuses.All(a => a.LastSeen > (DateTime.UtcNow - TimeSpan.FromMinutes(2))), "Expired apis should have been removed");
    }
}
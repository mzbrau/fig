using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Contracts.Status;
using Fig.Test.Common;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public class ApiStatusTests : IntegrationTestBase
{
    [Test]
    public async Task ShallGetApiStatus()
    {
        var statuses = await GetAllApiStatuses();
        
        Assert.That(statuses.Count, Is.AtLeast(1));
        Assert.That(statuses.All(a => a.Hostname == Environment.MachineName), "Status should be from this api");
        Assert.That(statuses.All(a => a.LastSeen > (DateTime.UtcNow - TimeSpan.FromMinutes(2))), "Expired apis should have been removed");
    }
    
    private async Task<List<ApiStatusDataContract>> GetAllApiStatuses()
    {
        const string uri = "/apistatus";
        var result = await ApiClient.Get<List<ApiStatusDataContract>>(uri);
        
        if (result == null)
            throw new ApplicationException($"Expected non null result for get for URI {uri}");

        return result;
        
    }
}
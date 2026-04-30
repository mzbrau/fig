using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Fig.Contracts.CheckPoint;
using Fig.Mcp.ApiClient;
using Fig.Mcp.Tools;
using Fig.Test.Common;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Mcp;

public abstract class McpToolIntegrationTestBase : IntegrationTestBase
{
    protected IFigApiClient McpApiClient = null!;

    [SetUp]
    public override async Task Setup()
    {
        await base.Setup();

        var authResponse = await Login();

        var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authResponse.Token);

        McpApiClient = new FigApiClient(httpClient);
    }

    protected async Task<CheckPointCollectionDataContract> WaitForMcpCheckPoint(DateTime startTime, DateTime endTime)
    {
        CheckPointCollectionDataContract? collection = null;

        await WaitForCondition(async () =>
        {
            var result = await TimeMachineReadTools.ListCheckPoints(
                McpApiClient,
                startTime,
                endTime,
                CancellationToken.None);

            collection = JsonConvert.DeserializeObject<CheckPointCollectionDataContract>(result);
            return collection?.CheckPoints?.Any() == true;
        }, TimeSpan.FromSeconds(5), () => "Expected a time-machine checkpoint to be available through the MCP API.");

        return collection!;
    }
}

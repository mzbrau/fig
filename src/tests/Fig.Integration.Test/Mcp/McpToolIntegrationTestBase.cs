using System.Net.Http.Headers;
using System.Threading.Tasks;
using Fig.Mcp.ApiClient;
using Fig.Test.Common;
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
}

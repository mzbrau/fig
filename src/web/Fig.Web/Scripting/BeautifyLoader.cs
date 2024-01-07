using Fig.Common.NetStandard.Constants;

namespace Fig.Web.Scripting;

public class BeautifyLoader : IBeautifyLoader
{
    private readonly IHttpClientFactory _httpClientFactory;

    public BeautifyLoader(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    
    public async Task<string?> LoadBeautifyScript()
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientNames.WebApp);
            return await client.GetStringAsync("scripts/beautify.js");
        }
        catch (Exception e)
        {
            Console.WriteLine("Unable to load beautify script" + e);
        }

        return null;
    }
}
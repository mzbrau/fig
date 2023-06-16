namespace Fig.Test.Common;

public class CustomHttpClientFactory : IHttpClientFactory
{
    private readonly HttpClient _httpClient;
    
    public CustomHttpClientFactory(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public HttpClient CreateClient(string name)
    {
        return _httpClient;
    }
}
namespace Fig.Common.Factories;

public class HttpClientFactory : IHttpClientFactory
{
    public HttpClient Create(string baseAddress)
    {
        return new HttpClient()
        {
            BaseAddress = new Uri(baseAddress)
        };
    }
}
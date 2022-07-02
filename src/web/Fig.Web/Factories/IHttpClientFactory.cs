namespace Fig.Web.Factories;

public interface IHttpClientFactory
{
    HttpClient Create(string baseAddress);
}
namespace Fig.Common.Factories;

public interface IHttpClientFactory
{
    HttpClient Create(string baseAddress);
}
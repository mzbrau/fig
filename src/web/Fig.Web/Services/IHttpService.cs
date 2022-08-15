namespace Fig.Web.Services;

public interface IHttpService
{
    string BaseAddress { get; }
    
    Task<T?> Get<T>(string uri);
    
    Task Post(string uri, object value);
    
    Task<T?> Post<T>(string uri, object value);
    
    Task Put(string uri, object value);
    
    Task<T?> Put<T>(string uri, object? value);
    
    Task Delete(string uri);
    
    Task<T?> Delete<T>(string uri);
}
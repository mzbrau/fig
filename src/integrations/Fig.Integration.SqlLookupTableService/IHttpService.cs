namespace Fig.Integration.SqlLookupTableService;

public interface IHttpService
{
    Task LogIn();
    
    Task<T?> Get<T>(string uri);

    Task Post(string uri, object value);

    Task Put(string uri, object value);
}
namespace Fig.Web.Facades;

public interface IApiVersionFacade : IDisposable
{
    event EventHandler IsConnectedChanged;
    
    bool IsConnected { get; }
    
    DateTime? LastConnected { get; }
    
    string ApiAddress { get; }
    
    string? ApiVersion { get; }
}
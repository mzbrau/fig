namespace Fig.Api.Services;

public interface IDiagnosticsService : IDisposable
{
    void RegisterRequest();
    
    long TotalRequests { get; }
    
    double RequestsPerMinute { get; }
}
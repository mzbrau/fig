namespace Fig.Api.Services;

public interface IDiagnosticsService
{
    void RegisterRequest();
    
    long TotalRequests { get; }
    
    double RequestsPerMinute { get; }
}
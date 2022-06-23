using Fig.Api.Services;

namespace Fig.Api.Middleware;

public class RequestCountMiddleware
{
    private readonly RequestDelegate _next;

    public RequestCountMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context,
        IDiagnosticsService diagnosticsService)
    {
        diagnosticsService.RegisterRequest();

        await _next(context);
    }
}
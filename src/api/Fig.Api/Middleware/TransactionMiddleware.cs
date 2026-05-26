using System.Diagnostics;
using Fig.Api.Attributes;
using Fig.Api.ExtensionMethods;
using Microsoft.Extensions.Options;
using ISession = NHibernate.ISession;

namespace Fig.Api.Middleware;

public class TransactionMiddleware
{
    private const long SlowTransactionWarningMs = 2000;
    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<ApiSettings> _settings;
    private readonly ILogger<TransactionMiddleware> _logger;

    public TransactionMiddleware(RequestDelegate next, IOptionsMonitor<ApiSettings> settings, ILogger<TransactionMiddleware> logger)
    {
        _next = next;
        _settings = settings;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context, ISession session)
    {
        if (_settings.CurrentValue.DisableTransactionMiddleware || ShouldSkipTransaction(context))
        {
            await _next(context);
        }
        else
        {
            var watch = Stopwatch.StartNew();
            using var transaction = session.BeginTransaction();
            try
            {
                await _next(context);

                if (transaction.IsActive)
                {
                    await transaction.CommitAsync();
                }

                LogSlowTransaction(context, watch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                if (transaction.IsActive)
                {
                    await transaction.RollbackAsync();
                }

                LogTransactionRolledBack(context, watch.ElapsedMilliseconds, ex);
            
                throw;
            }
        }
    }

    private static bool ShouldSkipTransaction(HttpContext context)
    {
        return context.GetEndpoint()?.Metadata.GetMetadata<SkipTransactionAttribute>() is not null;
    }

    private void LogTransactionRolledBack(HttpContext context, long elapsedMs, Exception exception)
    {
        _logger.LogWarning(exception,
            "Request transaction rolled back after {ElapsedMs} ms for {Method} {Path} trace {TraceId}",
            elapsedMs,
            GetSanitizedMethod(context),
            GetSanitizedPath(context),
            context.TraceIdentifier.Sanitize());
    }

    private void LogSlowTransaction(HttpContext context, long elapsedMs)
    {
        if (elapsedMs < SlowTransactionWarningMs)
            return;

        _logger.LogWarning(
            "Slow request transaction committed after {ElapsedMs} ms for {Method} {Path} status {StatusCode} trace {TraceId}",
            elapsedMs,
            GetSanitizedMethod(context),
            GetSanitizedPath(context),
            context.Response.StatusCode,
            context.TraceIdentifier.Sanitize());
    }

    private static string GetSanitizedMethod(HttpContext context)
    {
        return context.Request.Method.Sanitize();
    }

    private static string GetSanitizedPath(HttpContext context)
    {
        return (context.Request.Path.Value ?? string.Empty).Sanitize();
    }
}
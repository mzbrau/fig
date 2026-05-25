using System.Diagnostics;
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
        if (!_settings.CurrentValue.DisableTransactionMiddleware)
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

                LogSlowTransaction(context, watch.ElapsedMilliseconds, false);
            }
            catch (Exception ex)
            {
                if (transaction.IsActive)
                {
                    await transaction.RollbackAsync();
                }

                _logger.LogWarning(ex,
                    "Request transaction rolled back after {ElapsedMs} ms for {Method} {Path} trace {TraceId}",
                    watch.ElapsedMilliseconds,
                    context.Request.Method,
                    context.Request.Path,
                    context.TraceIdentifier);
            
                throw;
            }
        }
        else
        {
            await _next(context);
        }
    }

    private void LogSlowTransaction(HttpContext context, long elapsedMs, bool rollback)
    {
        if (elapsedMs < SlowTransactionWarningMs)
            return;

        _logger.LogWarning(
            "Slow request transaction {TransactionOutcome} after {ElapsedMs} ms for {Method} {Path} status {StatusCode} trace {TraceId}",
            rollback ? "rolled back" : "committed",
            elapsedMs,
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            context.TraceIdentifier);
    }
}
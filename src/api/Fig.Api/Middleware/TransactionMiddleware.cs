using Microsoft.Extensions.Options;
using ISession = NHibernate.ISession;

namespace Fig.Api.Middleware;

public class TransactionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<ApiSettings> _settings;

    public TransactionMiddleware(RequestDelegate next, IOptionsMonitor<ApiSettings> settings)
    {
        _next = next;
        _settings = settings;
    }

    public async Task Invoke(HttpContext context, ISession session)
    {
        if (!_settings.CurrentValue.DisableTransactionMiddleware)
        {
            using var transaction = session.BeginTransaction();
            try
            {
                await _next(context);

                if (transaction.IsActive)
                {
                    await transaction.CommitAsync();
                }
            }
            catch
            {
                if (transaction.IsActive)
                {
                    await transaction.RollbackAsync();
                }
            
                throw;
            }
        }
        else
        {
            await _next(context);
        }
    }
}
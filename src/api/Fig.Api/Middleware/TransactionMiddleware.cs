using ISession = NHibernate.ISession;

namespace Fig.Api.Middleware;

public class TransactionMiddleware
{
    private readonly RequestDelegate _next;

    public TransactionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, ISession session)
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
            await transaction.RollbackAsync();
            throw;
        }
    }
}
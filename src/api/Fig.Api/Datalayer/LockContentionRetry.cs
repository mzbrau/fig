using Fig.Api.ExtensionMethods;

namespace Fig.Api.Datalayer;

/// <summary>
/// Retries an operation when the failure is SQLite/SQL lock contention.
/// Defaults match the previous per-repository policy (3 attempts, 100ms * attempt).
/// </summary>
public static class LockContentionRetry
{
    public const int DefaultMaxAttempts = 3;
    public const int DefaultBaseDelayMs = 100;

    public static T Execute<T>(
        Func<T> action,
        int maxAttempts = DefaultMaxAttempts,
        int baseDelayMs = DefaultBaseDelayMs,
        Action<Exception, int>? onRetry = null)
    {
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return action();
            }
            catch (Exception ex) when (attempt < maxAttempts && ex.IsLockContention())
            {
                onRetry?.Invoke(ex, attempt);
                Thread.Sleep(baseDelayMs * attempt);
            }
        }

        throw new InvalidOperationException("LockContentionRetry.Execute should not reach this point.");
    }

    public static void Execute(
        Action action,
        int maxAttempts = DefaultMaxAttempts,
        int baseDelayMs = DefaultBaseDelayMs,
        Action<Exception, int>? onRetry = null)
    {
        Execute(() =>
        {
            action();
            return true;
        }, maxAttempts, baseDelayMs, onRetry);
    }

    public static async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        int maxAttempts = DefaultMaxAttempts,
        int baseDelayMs = DefaultBaseDelayMs,
        Action<Exception, int>? onRetry = null,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await action(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (attempt < maxAttempts && ex.IsLockContention())
            {
                onRetry?.Invoke(ex, attempt);
                await Task.Delay(baseDelayMs * attempt, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException("LockContentionRetry.ExecuteAsync should not reach this point.");
    }

    public static async Task ExecuteAsync(
        Func<CancellationToken, Task> action,
        int maxAttempts = DefaultMaxAttempts,
        int baseDelayMs = DefaultBaseDelayMs,
        Action<Exception, int>? onRetry = null,
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async ct =>
        {
            await action(ct).ConfigureAwait(false);
            return true;
        }, maxAttempts, baseDelayMs, onRetry, cancellationToken).ConfigureAwait(false);
    }
}

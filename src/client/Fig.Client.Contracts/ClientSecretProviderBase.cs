using Microsoft.Extensions.Logging;

namespace Fig.Client.Contracts;

public abstract class ClientSecretProviderBase<T> : IClientSecretProvider, IDisposable
{
    protected ILogger<T>? Logger;
    protected const string SecretKeyFormat = "FIG_{0}_SECRET";
    protected readonly SemaphoreSlim Semaphore = new(1, 1);
    protected readonly Dictionary<string, string> SecretCache = new();
    protected readonly bool AutoCreate;

    protected ClientSecretProviderBase(string name, bool? autoCreate = null)
    {
        Name = name;
        AutoCreate = DetermineAutoCreate(autoCreate);
    }

    private static bool DetermineAutoCreate(bool? autoCreate)
    {
        if (autoCreate.HasValue)
            return autoCreate.Value;

        // Check ASPNETCORE_ENVIRONMENT or DOTNET_ENVIRONMENT for Development
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                  ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        return string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);
    }

    public string Name { get; }

    public virtual bool IsEnabled => true;

    public void AddLogger(ILoggerFactory loggerFactory)
    {
        Logger = loggerFactory.CreateLogger<T>();
    }

    public async Task<string> GetSecret(string clientName)
    {
        if (SecretCache.TryGetValue(clientName, out var secret))
            return secret!;

        // Use semaphore to prevent race conditions when multiple threads try to create the same secret
        await Semaphore.WaitAsync();
        try
        {
            // Double-check pattern: verify secret wasn't created by another thread while waiting
            if (SecretCache.TryGetValue(clientName, out secret))
                return secret!;

            return await GetOrCreateSecretInternal(clientName);
        }
        finally
        {
            Semaphore.Release();
        }
    }

    protected abstract Task<string> GetOrCreateSecretInternal(string clientName);

    public virtual void Dispose()
    {
        Semaphore.Dispose();
    }
}
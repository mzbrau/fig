using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Fig.Client.Contracts;
using Microsoft.Extensions.Logging;

namespace Fig.Client.Aws
{
    public class AwsSecretProvider : ClientSecretProviderBase<AwsSecretProvider>
    {
        private readonly IAmazonSecretsManager _client;

        /// <summary>
        /// Creates a new AWS Secrets Manager secret provider using default credentials chain.
        /// </summary>
        /// <param name="regionEndpoint">The AWS region where secrets are stored</param>
        public AwsSecretProvider(RegionEndpoint regionEndpoint)
            : base("Aws")
        {
            _client = new AmazonSecretsManagerClient(regionEndpoint);
        }

        /// <summary>
        /// Creates a new AWS Secrets Manager secret provider with custom client.
        /// </summary>
        /// <param name="client">Custom AWS Secrets Manager client (useful for testing or custom configuration)</param>
        public AwsSecretProvider(IAmazonSecretsManager client)
            : base("Aws")
        {
            _client = client;
        }

        protected override async Task<string> GetOrCreateSecretInternal(string clientName)
        {
            var secretKey = string.Format(SecretKeyFormat, clientName.Replace(" ", "").ToUpper());
            Logger?.LogDebug("Attempting to retrieve AWS secret for key {SecretKey} (client: {ClientName})", secretKey,
                clientName);
            try
            {
                var response = await _client.GetSecretValueAsync(new GetSecretValueRequest
                {
                    SecretId = secretKey
                });
                if (!string.IsNullOrEmpty(response.SecretString))
                {
                    SecretCache[clientName] = response.SecretString;
                    Logger?.LogInformation(
                        "Successfully retrieved AWS secret for key {SecretKey} (client: {ClientName})", secretKey,
                        clientName);
                    return SecretCache[clientName];
                }
            }
            catch (ResourceNotFoundException)
            {
                Logger?.LogDebug("AWS secret not found for key {SecretKey} (client: {ClientName})", secretKey,
                    clientName);
            }
            catch (AmazonSecretsManagerException ex) when (IsTransientError(ex))
            {
                Logger?.LogWarning("Transient AWS error retrieving secret {SecretKey}: {Error} Retrying", secretKey,
                    ex.Message);
                await Task.Delay(1000);
                try
                {
                    var response = await _client.GetSecretValueAsync(new GetSecretValueRequest
                    {
                        SecretId = secretKey
                    });
                    if (!string.IsNullOrEmpty(response.SecretString))
                    {
                        SecretCache[clientName] = response.SecretString;
                        Logger?.LogInformation(
                            "Successfully retrieved AWS secret for key {SecretKey} (client: {ClientName}) after retry",
                            secretKey, clientName);
                        return SecretCache[clientName];
                    }
                }
                catch (ResourceNotFoundException)
                {
                    Logger?.LogDebug("AWS secret not found for key {SecretKey} after retry (client: {ClientName})",
                        secretKey, clientName);
                }
            }

            if (!AutoCreate)
            {
                Logger?.LogError("No secret found in AWS Secrets Manager with key: {SecretKey} (client: {ClientName})",
                    secretKey, clientName);
                throw new SecretNotFoundException($"No secret found in AWS Secrets Manager with key: {secretKey}");
            }

            return await CreateSecretWithRetry(clientName, secretKey);
        }

        private async Task<string> CreateSecretWithRetry(string clientName, string secretKey)
        {
            const int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var newSecret = Guid.NewGuid().ToString();
                    Logger?.LogDebug("Attempting to create AWS secret {SecretKey} (attempt {Attempt})", secretKey,
                        attempt);
                    await _client.CreateSecretAsync(new CreateSecretRequest
                    {
                        Name = secretKey,
                        SecretString = newSecret,
                        Description = $"Fig client secret for {secretKey}"
                    });
                    var verifyResponse = await _client.GetSecretValueAsync(new GetSecretValueRequest
                    {
                        SecretId = secretKey
                    });
                    if (verifyResponse.SecretString == newSecret)
                    {
                        SecretCache[clientName] = newSecret;
                        Logger?.LogInformation(
                            "Successfully created AWS secret for key {SecretKey} (client: {ClientName})", secretKey,
                            clientName);
                        return SecretCache[clientName];
                    }

                    if (!string.IsNullOrEmpty(verifyResponse.SecretString))
                    {
                        Logger?.LogWarning(
                            "AWS secret {SecretKey} was created concurrently by another process. Using existing value",
                            secretKey);
                        SecretCache[clientName] = verifyResponse.SecretString;
                        return SecretCache[clientName];
                    }
                }
                catch (ResourceExistsException)
                {
                    Logger?.LogWarning("AWS secret {SecretKey} already exists. Attempting to retrieve existing value",
                        secretKey);
                    try
                    {
                        var existingResponse = await _client.GetSecretValueAsync(new GetSecretValueRequest
                        {
                            SecretId = secretKey
                        });
                        if (!string.IsNullOrEmpty(existingResponse.SecretString))
                        {
                            SecretCache[clientName] = existingResponse.SecretString;
                            Logger?.LogInformation(
                                "Successfully retrieved existing AWS secret for key {SecretKey} after conflict (client: {ClientName})",
                                secretKey, clientName);
                            return SecretCache[clientName];
                        }
                    }
                    catch (AmazonSecretsManagerException ex2)
                    {
                        Logger?.LogError(
                            "Failed to read existing AWS secret {SecretKey} after ResourceExistsException: {Error}",
                            secretKey, ex2.Message);
                    }
                }
                catch (AmazonSecretsManagerException ex) when (IsTransientError(ex) && attempt < maxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                    Logger?.LogWarning(
                        "Transient AWS error creating secret {SecretKey}: {Error} Retrying in {DelaySeconds} seconds",
                        secretKey, ex.Message, delay.TotalSeconds);
                    await Task.Delay(delay);
                    continue;
                }

                if (attempt == maxRetries)
                {
                    Logger?.LogError("Failed to create or retrieve AWS secret {SecretKey} after {MaxRetries} attempts",
                        secretKey, maxRetries);
                    throw new InvalidOperationException(
                        $"Failed to create or retrieve secret '{secretKey}' after {maxRetries} attempts");
                }
            }

            Logger?.LogError("Unexpected error creating AWS secret {SecretKey}", secretKey);
            throw new InvalidOperationException($"Unexpected error creating secret '{secretKey}'");
        }

        private static bool IsTransientError(AmazonSecretsManagerException ex)
        {
            return ex.ErrorCode == "Throttling" ||
                   ex.ErrorCode == "ServiceUnavailable" ||
                   ex.ErrorCode == "InternalServiceError" ||
                   (int)ex.StatusCode == 429 || // Too Many Requests
                   ex.StatusCode == System.Net.HttpStatusCode.InternalServerError ||
                   ex.StatusCode == System.Net.HttpStatusCode.BadGateway ||
                   ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                   ex.StatusCode == System.Net.HttpStatusCode.GatewayTimeout;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _client?.Dispose();
            }
        }

        public override void Dispose()
        {
            Dispose(true);
            base.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
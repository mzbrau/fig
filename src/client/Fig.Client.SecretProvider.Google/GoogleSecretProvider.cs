using System;
using System.Threading.Tasks;
using Google.Api.Gax.ResourceNames;
using Google.Cloud.SecretManager.V1;
using Google.Protobuf;
using Grpc.Core;
using Fig.Client.Contracts;
using Microsoft.Extensions.Logging;

namespace Fig.Client.Google
{
    public class GoogleSecretProvider : ClientSecretProviderBase<GoogleSecretProvider>
    {
        private readonly SecretManagerServiceClient _client;
        private readonly string _projectId;

        /// <summary>
        /// Creates a new Google Cloud Secret Manager secret provider using default credentials.
        /// </summary>
        /// <param name="projectId">The Google Cloud project ID where secrets are stored</param>
        public GoogleSecretProvider(string projectId)
            : base("Google")
        {
            _projectId = projectId ?? throw new ArgumentNullException(nameof(projectId));
            _client = SecretManagerServiceClient.Create();
        }

        /// <summary>
        /// Creates a new Google Cloud Secret Manager secret provider with custom client.
        /// </summary>
        /// <param name="client">Custom Secret Manager client (useful for testing or custom configuration)</param>
        /// <param name="projectId">The Google Cloud project ID where secrets are stored</param>
        public GoogleSecretProvider(SecretManagerServiceClient client, string projectId)
            : base("Google")
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _projectId = projectId ?? throw new ArgumentNullException(nameof(projectId));
        }

        protected override async Task<string> GetOrCreateSecretInternal(string clientName)
        {
            var secretKey = string.Format(SecretKeyFormat, clientName.Replace(" ", "").ToUpper());
            Logger?.LogDebug("Attempting to retrieve Google secret for key {SecretKey} (client: {ClientName})",
                secretKey, clientName);
            try
            {
                var response = await _client.AccessSecretVersionAsync(new AccessSecretVersionRequest
                {
                    Name = SecretVersionName.FromProjectSecretSecretVersion(_projectId, secretKey, "latest").ToString()
                });
                if (response?.Payload?.Data != null)
                {
                    var secret = response.Payload.Data.ToStringUtf8();
                    if (!string.IsNullOrEmpty(secret))
                    {
                        SecretCache[clientName] = secret;
                        Logger?.LogInformation(
                            "Successfully retrieved Google secret for key {SecretKey} (client: {ClientName})",
                            secretKey, clientName);
                        return SecretCache[clientName];
                    }
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                Logger?.LogDebug("Google secret not found for key {SecretKey} (client: {ClientName})", secretKey,
                    clientName);
            }
            catch (RpcException ex) when (IsTransientError(ex))
            {
                Logger?.LogWarning("Transient Google error retrieving secret {SecretKey}: {Error} Retrying", secretKey,
                    ex.Message);
                await Task.Delay(1000);
                try
                {
                    var response = await _client.AccessSecretVersionAsync(new AccessSecretVersionRequest
                    {
                        Name = SecretVersionName.FromProjectSecretSecretVersion(_projectId, secretKey, "latest")
                            .ToString()
                    });
                    if (response?.Payload?.Data != null)
                    {
                        var secret = response.Payload.Data.ToStringUtf8();
                        if (!string.IsNullOrEmpty(secret))
                        {
                            SecretCache[clientName] = secret;
                            Logger?.LogInformation(
                                "Successfully retrieved Google secret for key {SecretKey} (client: {ClientName}) after retry",
                                secretKey, clientName);
                            return SecretCache[clientName];
                        }
                    }
                }
                catch (RpcException retryEx) when (retryEx.StatusCode == StatusCode.NotFound)
                {
                    Logger?.LogDebug("Google secret not found for key {SecretKey} after retry (client: {ClientName})",
                        secretKey, clientName);
                }
            }

            if (!AutoCreate)
            {
                Logger?.LogError(
                    "No secret found in Google Cloud Secret Manager with key: {SecretKey} (client: {ClientName})",
                    secretKey, clientName);
                throw new SecretNotFoundException(
                    $"No secret found in Google Cloud Secret Manager with key: {secretKey}");
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
                    Logger?.LogDebug("Attempting to create Google secret {SecretKey} (attempt {Attempt})", secretKey,
                        attempt);
                    var secret = await _client.CreateSecretAsync(new CreateSecretRequest
                    {
                        Parent = ProjectName.FromProject(_projectId).ToString(),
                        SecretId = secretKey,
                        Secret = new Secret
                        {
                            Replication = new Replication
                            {
                                Automatic = new Replication.Types.Automatic()
                            },
                            Labels = { { "managed-by", "fig" } }
                        }
                    });
                    await _client.AddSecretVersionAsync(new AddSecretVersionRequest
                    {
                        Parent = secret.Name,
                        Payload = new SecretPayload
                        {
                            Data = ByteString.CopyFromUtf8(newSecret)
                        }
                    });
                    var verifyResponse = await _client.AccessSecretVersionAsync(new AccessSecretVersionRequest
                    {
                        Name = SecretVersionName.FromProjectSecretSecretVersion(_projectId, secretKey, "latest")
                            .ToString()
                    });
                    if (verifyResponse?.Payload?.Data != null)
                    {
                        var retrievedSecret = verifyResponse.Payload.Data.ToStringUtf8();
                        if (retrievedSecret == newSecret)
                        {
                            SecretCache[clientName] = newSecret;
                            Logger?.LogInformation(
                                "Successfully created Google secret for key {SecretKey} (client: {ClientName})",
                                secretKey, clientName);
                            return SecretCache[clientName];
                        }

                        if (!string.IsNullOrEmpty(retrievedSecret))
                        {
                            Logger?.LogWarning(
                                "Google secret {SecretKey} was created concurrently by another process. Using existing value",
                                secretKey);
                            SecretCache[clientName] = retrievedSecret;
                            return SecretCache[clientName];
                        }
                    }
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
                {
                    Logger?.LogWarning(
                        "Google secret {SecretKey} already exists. Attempting to retrieve existing value", secretKey);
                    try
                    {
                        var existingResponse = await _client.AccessSecretVersionAsync(new AccessSecretVersionRequest
                        {
                            Name = SecretVersionName.FromProjectSecretSecretVersion(_projectId, secretKey, "latest")
                                .ToString()
                        });
                        if (existingResponse?.Payload?.Data != null)
                        {
                            var secret = existingResponse.Payload.Data.ToStringUtf8();
                            if (!string.IsNullOrEmpty(secret))
                            {
                                SecretCache[clientName] = secret;
                                Logger?.LogInformation(
                                    "Successfully retrieved existing Google secret for key {SecretKey} after conflict (client: {ClientName})",
                                    secretKey, clientName);
                                return SecretCache[clientName];
                            }
                        }
                    }
                    catch (RpcException ex2)
                    {
                        Logger?.LogError("Failed to read existing Google secret {SecretKey} after conflict: {Error}",
                            secretKey, ex2.Message);
                    }
                }
                catch (RpcException ex) when (IsTransientError(ex) && attempt < maxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                    Logger?.LogWarning(
                        "Transient Google error creating secret {SecretKey}: {Error} Retrying in {DelaySeconds} seconds",
                        secretKey, ex.Message, delay.TotalSeconds);
                    await Task.Delay(delay);
                    continue;
                }

                if (attempt == maxRetries)
                {
                    Logger?.LogError(
                        "Failed to create or retrieve Google secret {SecretKey} after {MaxRetries} attempts", secretKey,
                        maxRetries);
                    throw new InvalidOperationException(
                        $"Failed to create or retrieve secret '{secretKey}' after {maxRetries} attempts");
                }
            }

            Logger?.LogError("Unexpected error creating Google secret {SecretKey}", secretKey);
            throw new InvalidOperationException($"Unexpected error creating secret '{secretKey}'");
        }

        private static bool IsTransientError(RpcException ex)
        {
            return ex.StatusCode == StatusCode.ResourceExhausted || // Rate limiting
                   ex.StatusCode == StatusCode.Unavailable || // Service unavailable
                   ex.StatusCode == StatusCode.Internal || // Internal server error
                   ex.StatusCode == StatusCode.DeadlineExceeded; // Timeout
        }
    }
}
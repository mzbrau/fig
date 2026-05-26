using Fig.Contracts.ApiSecret;

namespace Fig.Api.Services;

public interface IEncryptionMigrationService : IAuthenticatedService
{
    Task PerformMigration();

    Task<ApiSecretRotationStatusDataContract> GetStatus();
}
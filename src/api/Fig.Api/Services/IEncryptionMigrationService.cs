namespace Fig.Api.Services;

public interface IEncryptionMigrationService : IAuthenticatedService
{
    void PerformMigration();
}
namespace Fig.Api.Services;

public interface IEncryptionMigrationService : IAuthenticatedService
{
    Task PerformMigration();
}
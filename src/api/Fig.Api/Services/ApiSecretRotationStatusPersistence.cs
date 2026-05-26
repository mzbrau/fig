namespace Fig.Api.Services;

public static class ApiSecretRotationStatusPersistence
{
    public static string ToStorageValue(ApiSecretRotationMigrationStatus status)
    {
        return status.ToString();
    }

    public static ApiSecretRotationMigrationStatus Parse(string? value)
    {
        return Enum.TryParse<ApiSecretRotationMigrationStatus>(value, out var status)
            ? status
            : ApiSecretRotationMigrationStatus.PendingMigration;
    }
}

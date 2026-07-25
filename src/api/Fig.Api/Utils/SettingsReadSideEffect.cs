namespace Fig.Api.Utils;

public record SettingsReadSideEffect(
    Guid ClientId,
    string ClientName,
    string? Instance,
    Guid RunSessionId,
    DateTime LoadedUtc);

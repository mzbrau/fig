namespace Fig.Api.Datalayer.Repositories;

public record SettingClientReadFailure(string ClientName, string? Instance, string? SettingName, string Message, Exception Exception);

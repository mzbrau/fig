// ReSharper disable CollectionNeverUpdated.Global Set by appSettings.json
namespace Fig.Api;

public class ApiSettings
{
    public string Secret { get; set; } = null!;

    public long TokenLifeMinutes { get; set; }

    public List<string>? PreviousSecrets { get; set; }

    public List<string>? WebClientAddresses { get; set; }

    public bool ForceAdminDefaultPasswordChange { get; set; }
}
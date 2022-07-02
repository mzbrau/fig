namespace Fig.Api;

public class ApiSettings
{
    public string Secret { get; set; }

    public long TokenLifeMinutes { get; set; }

    public List<string>? PreviousSecrets { get; set; }

    public List<string>? WebClientAddresses { get; set; }
}
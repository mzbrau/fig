namespace Fig.Contracts.SettingClients;

/// <summary>
/// Body for the deferred client description PUT endpoint.
/// </summary>
public class ClientDescriptionUpdateDataContract
{
    public ClientDescriptionUpdateDataContract(string description)
    {
        Description = description;
    }

    public string Description { get; }
}

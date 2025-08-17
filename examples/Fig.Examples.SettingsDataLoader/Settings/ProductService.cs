using Fig.Client;
using Fig.Client.Abstractions.Attributes;

namespace Fig.Examples.SettingsDataLoader.Settings;

public class ProductService : SettingsBase
{
    public override string ClientDescription => "Sample Product Service";

    [Setting("This is the address of a website")]
    public string WebsiteAddress { get; set; } = "http://www.google.com";

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}
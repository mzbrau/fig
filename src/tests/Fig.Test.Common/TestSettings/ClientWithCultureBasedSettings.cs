using Fig.Client.Attributes;

namespace Fig.Test.Common.TestSettings;

public class ClientWithCultureBasedSettings : TestSettingsBase
{
    public override string ClientName => nameof(ClientWithCultureBasedSettings);

    public override string ClientDescription => "ClientWithCultureBasedSettings";
    
    [Setting("normal double")]
    public double NormalDouble { get; set; } = 5.6;

    [Setting("nullable double")] 
    public double? NullableDouble { get; set; } = 99.99;

    [Setting("DateTime")] 
    public DateTime DateTime { get; set; } = new DateTime(2024, 3, 3, 3, 3, 3);

    [Setting("Timespan")] 
    public TimeSpan Timespan { get; set; } = TimeSpan.FromMinutes(10);
    
    [Setting("Timespan", defaultValueMethodName: nameof(GetItems))] 
    public required List<Item> Items { get; set; }

    public static List<Item> GetItems()
    {
        return
        [
            new Item
            {
                Height = 4,
                Weight = 9
            },
            new Item
            {
                Height = 4.5,
                Weight = 7.3
            },
            new Item
            {
                Height = 10.5,
                Weight = null
            }
        ];
    }

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}

public class Item
{
    public double Height { get; set; }
    
    public double? Weight { get; set; }
}
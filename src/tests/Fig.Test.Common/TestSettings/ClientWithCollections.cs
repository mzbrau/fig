using Fig.Client.Attributes;
using Fig.Client.Enums;

namespace Fig.Test.Common.TestSettings;

public class ClientWithCollections : TestSettingsBase
{
    public override string ClientName => "ClientWithDataGrids";
    public override string ClientDescription => "Client with data grids";

    [Setting("AnimalNames", defaultValueMethodName: nameof(DefaultEmptyString))]
    [LookupTable("AnimalNames", LookupSource.UserDefined)]
    public required List<string> AnimalNames { get; set; }
    
    [Setting("AnimalDetails")]
    [LookupTable("AnimalNames", LookupSource.UserDefined)]
    public List<AnimalDetail>? AnimalDetails { get; set; }
    
    [Setting("CityNames")]
    [ValidValues("Melbourne", "Sydney", "Adelaide")]
    public List<string>? CityNames { get; set; }
    
    [Setting("CityDetails")]
    public List<CityDetail>? CityDetails { get; set; }

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }

    public static List<string> DefaultEmptyString()
    {
        return new List<string>();
    }
}

public class AnimalDetail
{
    public string Name { get; set; } = null!;

    public string Category { get; set; } = null!;
    
    public int HeightCm { get; set; }
    
    [ValidValues("Meat", "Cheese")]
    public List<string>? FavouriteFoods { get; set; }
}

public class CityDetail
{
    [ValidValues("London", "Paris", "Berlin")]
    public string Name { get; set; } = null!;

    public string Country { get; set; } = null!;
    
    public Size Size { get; set; }
}

public enum Size
{
    Small,
    Medium,
    Large
}
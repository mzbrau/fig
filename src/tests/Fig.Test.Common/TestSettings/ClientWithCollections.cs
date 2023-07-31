using Fig.Client;
using Fig.Client.Attributes;

namespace Fig.Test.Common.TestSettings;

public class ClientWithCollections : SettingsBase
{
    public override string ClientName => "ClientWithDataGrids";
    public override string ClientDescription => "Client with data grids";

    [Setting("AnimalNames")]
    [LookupTable("AnimalNames")]
    public List<string> AnimalNames { get; set; }
    
    [Setting("AnimalDetails")]
    [LookupTable("AnimalNames")]
    public List<AnimalDetail> AnimalDetails { get; set; }
    
    [Setting("CityNames")]
    [ValidValues("Melbourne", "Sydney", "Adelaide")]
    public List<string> CityNames { get; set; }
    
    [Setting("CityDetails")]
    public List<CityDetail> CityDetails { get; set; }
}

public class AnimalDetail
{
    public string Name { get; set; } = null!;

    public string Category { get; set; } = null!;
    
    public int HeightCm { get; set; }
}

public class CityDetail
{
    [ValidValues("London", "Paris", "Berlin")]
    public string Name { get; set; } = null!;

    public string Country { get; set; } = null!;
}
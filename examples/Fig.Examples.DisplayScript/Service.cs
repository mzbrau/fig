using Fig.Client.Attributes;

namespace Fig.Examples.DisplayScript;

public class Service
{
    public string Name { get; set; }
    
    [ValidValues("Placeholder")]
    public string Group { get; set; }
    
    [ValidValues("200OK", "Custom String")]
    public string ValidationType { get; set; }
    
    public string CustomString { get; set; }
}
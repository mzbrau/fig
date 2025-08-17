using Fig.Client.Abstractions.Attributes;

namespace Fig.Examples.DisplayScript;

public class Service
{
    public string Name { get; set; } = string.Empty;
    
    [ValidValues("Placeholder")]
    public string Group { get; set; } = string.Empty;
    
    [ValidValues("200OK", "Custom String")]
    public string ValidationType { get; set; } = string.Empty;
    
    public string CustomString { get; set; } = string.Empty;
}
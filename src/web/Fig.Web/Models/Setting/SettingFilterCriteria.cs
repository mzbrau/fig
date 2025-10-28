namespace Fig.Web.Models.Setting;

public class SettingFilterCriteria
{
    public bool? Advanced { get; set; }
    public string? Category { get; set; }
    public string? Classification { get; set; }
    public bool? Secret { get; set; }
    public bool? Valid { get; set; }
    public bool? Modified { get; set; }
    public List<string> GeneralSearchTerms { get; set; } = new();

    public bool IsEmpty => 
        Advanced == null && 
        Category == null &&
        Classification == null &&
        Secret == null && 
        Valid == null && 
        Modified == null && 
        !GeneralSearchTerms.Any();

    
}
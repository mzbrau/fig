namespace Fig.Web.Models.ImportExport;

public class DeferredImportClientModel
{
    public string Name { get; set; }
    
    public string? Instance { get; set; }
    
    public int SettingCount { get; set; }
    
    public string RequestingUser { get; set; }
}
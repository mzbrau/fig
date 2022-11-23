namespace Fig.Web.Models.ImportExport;

public class DeferredImportClientModel
{
    public DeferredImportClientModel(string name, string? instance, int settingCount, string requestingUser)
    {
        Name = name;
        Instance = instance;
        SettingCount = settingCount;
        RequestingUser = requestingUser;
    }

    public string Name { get; }
    
    public string? Instance { get; }
    
    public int SettingCount { get; }
    
    public string RequestingUser { get; }
}
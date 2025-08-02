namespace Fig.Web.Models.ImportExport;

public class ClientSelectionModel
{
    public ClientSelectionModel(string name, string? instance, int settingCount, bool isSelected = false)
    {
        Name = name;
        Instance = instance;
        SettingCount = settingCount;
        IsSelected = isSelected;
    }

    public string Name { get; }
    
    public string? Instance { get; }
    
    public int SettingCount { get; }
    
    public bool IsSelected { get; set; }
    
    public string DisplayName => string.IsNullOrEmpty(Instance) ? Name : $"{Name} [{Instance}]";
    
    public string Identifier => $"{Name}-{Instance}";
}

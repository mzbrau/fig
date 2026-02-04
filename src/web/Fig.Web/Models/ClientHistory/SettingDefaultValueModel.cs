namespace Fig.Web.Models.ClientHistory;

public class SettingDefaultValueModel
{
    public string Name { get; set; } = string.Empty;
    
    public string? DefaultValue { get; set; }

    public bool Advanced { get; set; }
}

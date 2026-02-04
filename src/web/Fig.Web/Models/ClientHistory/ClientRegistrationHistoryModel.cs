namespace Fig.Web.Models.ClientHistory;

public class ClientRegistrationHistoryModel
{
    public Guid Id { get; set; }
    
    public DateTime RegistrationDateUtc { get; set; }
    
    public string ClientName { get; set; } = string.Empty;
    
    public string ClientVersion { get; set; } = string.Empty;
    
    public List<SettingDefaultValueModel> Settings { get; set; } = new();
}

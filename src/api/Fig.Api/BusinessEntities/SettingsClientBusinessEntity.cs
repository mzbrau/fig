namespace Fig.Api.BusinessEntities;

public class SettingsClientBusinessEntity
{
    public string Name { get; set; }
    
    public string ClientSecret { get; set; }
    
    public SettingQualifiersBusinessEntity Qualifiers { get; set; }
    
    public List<SettingBusinessEntity> Settings { get; set; }
}
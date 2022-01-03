namespace Fig.Contracts.Settings
{
    public class SettingRequestDataContract
    {
        public string ClientName { get; set; }
        
        public string ClientSecret { get; set; }
        
        public SettingQualifiersDataContract Qualifiers { get; set; }
    }
}
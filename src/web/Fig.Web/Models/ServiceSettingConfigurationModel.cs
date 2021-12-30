namespace Fig.Web.Models
{
    public class ServiceSettingConfigurationModel
    {
        public string Name { get; set; }

        public List<SettingConfigurationModel> Settings { get; set; }
    }
}

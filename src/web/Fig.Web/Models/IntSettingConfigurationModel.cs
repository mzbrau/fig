namespace Fig.Web.Models
{
    public class IntSettingConfigurationModel : SettingConfigurationModel
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public int Value { get; set; }
    }
}

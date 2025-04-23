namespace Fig.Contracts.Settings
{
    public class SettingDataContract
    {
        public SettingDataContract(
            string name,
            SettingValueBaseDataContract? value,
            bool isSecret = false)
        {
            Name = name;
            Value = value;
            IsSecret = isSecret;
        }

        public string Name { get; set; }
        
        public bool IsSecret { get; set; }
        
        public SettingValueBaseDataContract? Value { get; set; }
    }
}
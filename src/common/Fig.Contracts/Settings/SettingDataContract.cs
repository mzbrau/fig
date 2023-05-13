namespace Fig.Contracts.Settings
{
    public class SettingDataContract
    {
        public SettingDataContract(
            string name, 
            SettingValueBaseDataContract? value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; set; }
        
        public SettingValueBaseDataContract? Value { get; set; }
    }
}
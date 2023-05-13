namespace Fig.Contracts.ImportExport
{
    public class SettingValueExportDataContract
    {
        public SettingValueExportDataContract(
            string name, 
            object? value)
        {
            Name = name;
            Value = value;
        }
        
        public string Name { get; }

        public object? Value { get; internal set; }
    }
}
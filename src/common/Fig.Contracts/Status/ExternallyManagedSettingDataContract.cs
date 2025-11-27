namespace Fig.Contracts.Status
{
    public class ExternallyManagedSettingDataContract
    {
        public ExternallyManagedSettingDataContract(string name, object? value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }

        public object? Value { get; }
    }
}

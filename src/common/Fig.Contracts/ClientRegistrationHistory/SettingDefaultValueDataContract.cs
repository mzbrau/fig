namespace Fig.Contracts.ClientRegistrationHistory;

public class SettingDefaultValueDataContract
{
    public SettingDefaultValueDataContract()
    {
    }

    public SettingDefaultValueDataContract(string name, string? defaultValue)
    {
        Name = name;
        DefaultValue = defaultValue;
    }

    public SettingDefaultValueDataContract(string name, string? defaultValue, bool advanced)
    {
        Name = name;
        DefaultValue = defaultValue;
        Advanced = advanced;
    }

    public string Name { get; set; } = string.Empty;

    public string? DefaultValue { get; set; }

    public bool Advanced { get; set; }
}

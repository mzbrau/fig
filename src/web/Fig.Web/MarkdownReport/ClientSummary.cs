using Fig.Web.Attributes;

namespace Fig.Web.MarkdownReport;

public class ClientSummary
{
    public ClientSummary(string name, int settingCount, int instanceCount)
    {
        Name = name;
        SettingCount = settingCount;
        InstanceCount = instanceCount;
    }

    [Order(1)]
    [Sort]
    public string Name { get; set; }
    
    [Order(2)]
    public int SettingCount { get; set; }
    
    [Order(3)]
    public int InstanceCount { get; set; }
}
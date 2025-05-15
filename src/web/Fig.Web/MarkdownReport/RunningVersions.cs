using Fig.Web.Attributes;

namespace Fig.Web.MarkdownReport;

public class RunningVersions
{
    public RunningVersions(string name, string? version, string? figVersion)
    {
        Name = name;
        Version = version;
        FigVersion = figVersion;
    }

    [Order(1)]
    [Sort]
    public string Name { get; set; }
    
    [Order(2)]
    public string? Version { get; set; }
    
    [Order(3)]
    public string? FigVersion { get; set; }
}
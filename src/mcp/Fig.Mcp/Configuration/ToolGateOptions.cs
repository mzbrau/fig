namespace Fig.Mcp.Configuration;

public class ToolGateOptions
{
    // Read operations - enabled by default
    public bool ReadSettings { get; set; } = true;
    public bool ReadEvents { get; set; } = true;
    public bool ReadSessions { get; set; } = true;
    public bool ReadHistory { get; set; } = true;

    // Write operations - disabled by default
    public bool WriteSettings { get; set; }
    public bool ManageClients { get; set; }
    public bool DeleteClients { get; set; }
    public bool ManageUsers { get; set; }
    public bool ManageWebHooks { get; set; }
    public bool ManageLookupTables { get; set; }
    public bool ManageScheduling { get; set; }
    public bool ManageTimeMachine { get; set; }
    public bool ExecuteCustomActions { get; set; }
    public bool ImportExportData { get; set; }
    public bool ManageConfiguration { get; set; }
}

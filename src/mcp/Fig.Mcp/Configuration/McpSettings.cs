namespace Fig.Mcp.Configuration;

public class McpSettings
{
    public string FigApiBaseUrl { get; set; } = "https://localhost:7281";
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = string.Empty;
    public string Transport { get; set; } = "stdio";
    public ToolGateOptions ToolGates { get; set; } = new();
}

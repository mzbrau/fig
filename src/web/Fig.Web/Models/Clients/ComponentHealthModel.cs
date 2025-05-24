using Fig.Contracts.Health;

namespace Fig.Web.Models.Clients;

public class ComponentHealthModel
{
    public ComponentHealthModel(string name, FigHealthStatus status, string? message)
    {
        Name = name;
        Status = status;
        Message = message;
    }

    public string Name { get; }
    
    public FigHealthStatus Status { get; }
    
    public string? Message { get; }
}
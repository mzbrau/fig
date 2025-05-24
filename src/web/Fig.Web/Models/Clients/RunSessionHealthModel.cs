using Fig.Contracts.Health;

namespace Fig.Web.Models.Clients;

public class RunSessionHealthModel
{
    public RunSessionHealthModel(FigHealthStatus status, List<ComponentHealthModel> components)
    {
        Status = status;
        Components = components;
    }
    
    public FigHealthStatus Status { get; }
    
    public List<ComponentHealthModel> Components { get; }
}